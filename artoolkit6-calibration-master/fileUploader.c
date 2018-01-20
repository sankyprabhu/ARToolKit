/*
 *  fileUploader.c
 *  ARToolKit6
 *
 *  This file is part of ARToolKit.
 *
 *  Copyright 2015-2017 Daqri LLC. All Rights Reserved.
 *  Copyright 2013-2015 ARToolworks, Inc. All Rights Reserved.
 *
 *  Author(s): Philip Lamb
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 */


#include "fileUploader.h"

#include <stdio.h>
#include <string.h>
#include <stdbool.h>
#include <curl/curl.h>
#include <dirent.h> // opendir(), readdir(), closedir()
#include <sys/param.h> // MAXPATHLEN
#include <sys/stat.h> // struct stat, stat()
#include <pthread.h>

#include <AR6/AR/ar.h>
#include <AR6/ARUtil/thread_sub.h>
#include <AR6/ARUtil/file_utils.h> // mkdir_p()


static void *fileUploader(THREAD_HANDLE_T *threadHandle);

struct _FILE_UPLOAD_HANDLE {
    char                *queueDirPath;
    char                *formExtension;
    char                *formPostURL;
    THREAD_HANDLE_T     *uploadThread;
    char				 uploadStatus[UPLOAD_STATUS_BUFFER_LEN];
    bool                 uploadStatusHide; // Should check whether time for upload status to be hidden has arrived.
    struct timeval       uploadStatusHideAtTime; // The time at which upload status should be hidden.
    struct timeval       uploadStatusHideAfterSecs; // The number of seconds the user asked  for the status to be shown.
    pthread_mutex_t      uploadStatusLock;
};

// ---------------------------------------------------------------------------

static char *get_buff(char *buf, int n, FILE *fp, int skipblanks)
{
    char *ret;

    do {
        ret = fgets(buf, n, fp);
        if (ret == NULL) return (NULL); // EOF or error.

        // Remove NLs and CRs from end of string.
        size_t l = strlen(buf);
        while (l > 0) {
            if (buf[l - 1] != '\n' && buf[l - 1] != '\r') break;
            l--;
            buf[l] = '\0';
        }
    } while (buf[0] == '#' || (skipblanks && buf[0] == '\0')); // Reject comments and blank lines.

    return (ret);
}

static bool getNextFileInQueueWithExtension(const char *queueDir, const char *ext, char *buf, int len)
{
	DIR *dirp ;
	struct dirent *direntp;

	if (!buf || !ext) return (false);

	if (!(dirp = opendir(queueDir))) {
		ARLOGe("Error opening upload queue dir '%s'.\n", queueDir);
        ARLOGperror(NULL);
    	return (false);
	}

	*buf = '\0';
	while ((direntp = readdir(dirp))) {
		char *ext0 = arUtilGetFileExtensionFromPath(direntp->d_name, true);
		if (!ext0) continue;
		if (strcmp(ext0, ext) == 0) {
    		free(ext0);
    		snprintf(buf, len, "%s/%s", queueDir, direntp->d_name);
    		break;
		}
		free(ext0);
	}

	closedir(dirp);

	return (*buf != '\0');
}

// ---------------------------------------------------------------------------

FILE_UPLOAD_HANDLE_t *fileUploaderInit(const char *queueDirPath, const char *formExtension, const char *formPostURL, const float statusHideAfterSecs)
{
    FILE_UPLOAD_HANDLE_t *handle;
    
    if (!formExtension || !formPostURL) return (NULL);
    
    if (!(handle = (FILE_UPLOAD_HANDLE_t *)calloc(1, sizeof(FILE_UPLOAD_HANDLE_t)))) {
        ARLOGe("Out of memory!\n");
        return (NULL);
    }
    
    if (queueDirPath) handle->queueDirPath = strdup(queueDirPath);
    handle->formExtension = strdup(formExtension);
    handle->formPostURL = strdup(formPostURL);

    // Convert float time delta in seconds to a struct timeval.
	time_t secs = (time_t)statusHideAfterSecs;
	suseconds_t usecs = (suseconds_t)((statusHideAfterSecs - (float)secs)*1000000.0f);
    handle->uploadStatusHideAfterSecs.tv_sec = secs;
    handle->uploadStatusHideAfterSecs.tv_usec = usecs;

    // CURL init.
    if (curl_global_init(CURL_GLOBAL_DEFAULT) != CURLE_OK) {
    	ARLOGe("Unable to init libcurl.\n");
        free(handle);
    	return (NULL);
    }
    
    pthread_mutex_init(&(handle->uploadStatusLock), NULL);

    // Spawn the file upload worker thread.
    handle->uploadThread = threadInit(0, handle, fileUploader);
    
    return (handle);
}

void fileUploaderFinal(FILE_UPLOAD_HANDLE_t **handle_p)
{
    if (!handle_p || !*handle_p) return;
    
    if ((*handle_p)->uploadThread) {
    	threadWaitQuit((*handle_p)->uploadThread);
    	threadFree(&((*handle_p)->uploadThread));
    }

    pthread_mutex_destroy(&((*handle_p)->uploadStatusLock));

    // CURL final.
    curl_global_cleanup();

    if ((*handle_p)->queueDirPath) free((*handle_p)->queueDirPath);
    free((*handle_p)->formExtension);
    free((*handle_p)->formPostURL);
    free(*handle_p);
    *handle_p = NULL;
}

bool fileUploaderCreateQueueDir(const char *queueDirPath)
{
    if (!queueDirPath) return (false);

    int queueDirExists = test_d(queueDirPath);
    if (queueDirExists == -1) {
        // Some error other than "not found" occurred. Fail.
        ARLOGe("Error looking for queue directory '%s'.\n", queueDirPath);
        ARLOGperror(NULL);
        return false;
    } else if (!queueDirExists) {
        // Create the directory.
        if (mkdir_p(queueDirPath) == -1) {
            ARLOGe("Error creating queue directory '%s'.\n", queueDirPath);
            ARLOGperror(NULL);
            return false;
        }
    }

    ARLOGd("fileUploaderCreateQueueDir(\"%s\") OK.\n", queueDirPath);
    return (true);
}

bool fileUploaderTickle(FILE_UPLOAD_HANDLE_t *handle)
{
	if (!handle) return (false);

	threadStartSignal(handle->uploadThread);

	return (true);
}

static void *fileUploader(THREAD_HANDLE_T *threadHandle)
{
    FILE_UPLOAD_HANDLE_t *fileUploaderHandle;
    char *indexUploadPathname;
    char *filePathname;
#define BUFSIZE 1024
	char *buf;
    CURL *curlHandle = NULL;
    CURLcode curlErr;
	char curlErrorBuf[CURL_ERROR_SIZE];
	long http_response;


    ARLOGi("Start fileUploader thread.\n");
    fileUploaderHandle = (FILE_UPLOAD_HANDLE_t *)threadGetArg(threadHandle);
    arMalloc(indexUploadPathname, char, MAXPATHLEN);
    arMalloc(filePathname, char, MAXPATHLEN);
    arMalloc(buf, char, BUFSIZE);

    while (threadStartWait(threadHandle) == 0) {
    	ARLOGd("file uploader is GO\n");
    	pthread_mutex_lock(&(fileUploaderHandle->uploadStatusLock));
    	snprintf(fileUploaderHandle->uploadStatus, UPLOAD_STATUS_BUFFER_LEN, "Looking for files to upload...");
    	pthread_mutex_unlock(&(fileUploaderHandle->uploadStatusLock));

    	int uploadsDone = 0;
    	int errorCode = 0;

    	// Look for at least one unhandled uploadQueueFileName in the queue directory.
    	while (getNextFileInQueueWithExtension(fileUploaderHandle->queueDirPath, fileUploaderHandle->formExtension, indexUploadPathname, MAXPATHLEN)) {

        	pthread_mutex_lock(&(fileUploaderHandle->uploadStatusLock));
        	snprintf(fileUploaderHandle->uploadStatus, UPLOAD_STATUS_BUFFER_LEN, "Uploading file %d", uploadsDone + 1);
        	pthread_mutex_unlock(&(fileUploaderHandle->uploadStatusLock));

        	FILE *fp;
    		if (!(fp = fopen(indexUploadPathname, "rb"))) {
            	ARLOGe("Error opening upload queue file '%s'.\n", indexUploadPathname);
            	errorCode = -1;
    	    	break;
    		}

    	    //
    	    // cURL upload.
    	    //

    	    if (!curlHandle) {
    	    	curlHandle = curl_easy_init();
        	    if (!curlHandle) {
        	    	ARLOGe("Error initialising CURL.\n");
            	    errorCode = -1;
        	    	break;
        	    }
        	    curlErr = curl_easy_setopt(curlHandle, CURLOPT_ERRORBUFFER, curlErrorBuf);
        	    if (curlErr != CURLE_OK) {
        	    	ARLOGe("Error setting CURL error buffer: %s (%d)\n", curl_easy_strerror(curlErr), curlErr);
            	    errorCode = -1;
        	    	break;
        	    }
        	    
                // First, attempt a connection to a well-known site. If this fails, assume we have no
                // internet access at all.
                curlErr = curl_easy_setopt(curlHandle, CURLOPT_URL, "http://www.google.com");
                if (curlErr != CURLE_OK) {
                    ARLOGe("Error setting CURL URL: %s (%d)\n", curl_easy_strerror(curlErr), curlErr);
            	    errorCode = -1;
                    break;
                }
                curlErr = curl_easy_setopt(curlHandle, CURLOPT_NOBODY, 1L); // Headers only.
                if (curlErr != CURLE_OK) {
                    ARLOGe("Error setting CURL URL: %s (%d)\n", curl_easy_strerror(curlErr), curlErr);
            	    errorCode = -1;
                    break;
                }
                curlErr = curl_easy_perform(curlHandle);
                if (curlErr != CURLE_OK) {
                    // No need to report error, since we expect it (e.g.) when wifi and cell data are off.
                    // Typical first error in these cases is failure to resolve the hostname.
                    //LOGE("Error performing CURL network test: %s (%d). %s.\n", curl_easy_strerror(curlErr), curlErr, curlErrorBuf);
                    errorCode = 1;
                    break;
                }
    	    }

    		// Network OK, so proceed with upload.
    	    curlErr = curl_easy_setopt(curlHandle, CURLOPT_URL, fileUploaderHandle->formPostURL);
    	    if (curlErr != CURLE_OK) {
    	    	ARLOGe("Error setting CURL URL: %s (%d)\n", curl_easy_strerror(curlErr), curlErr);
            	errorCode = -1;
    	    	break;
    	    }

            // The commented-out section below disables SSL peer verification. Uncommenting this will make
            // https connections insecure, but will allow (for example) connections to a server using a
            // self-signed SSL certificate and when you have not provided CURL with a CAfile via
            // 'curl_easy_setopt(curlHandle, CURLOPT_CAPATH, capath);'.
            // (default capath: /etc/ssl/certs/ca-certificates.crt)
    	    //curlErr = curl_easy_setopt(curlHandle, CURLOPT_SSL_VERIFYPEER, 0L);
    	    //if (curlErr != CURLE_OK) {
    	    //	ARLOGe("Error setting CURL SSL options: %s (%d)\n", curl_easy_strerror(curlErr), curlErr);
            //	errorCode = -1;
    	    //	break;
    	    //}

    	    // Build the form.
    	    struct curl_httppost* post = NULL;
    	    struct curl_httppost* last = NULL;

    	    // Read lines from the file, creating curl parameters for each one.
    	    *filePathname = '\0';
    		while (get_buff(buf, BUFSIZE, fp, true)) {

    			// Locate first comma on line, and split the string there.
    			char *commaPos;
    			if (!(commaPos = strchr(buf, ','))) continue; // No comma found! Skip line.
    			*commaPos = '\0';

    			if (strcmp(buf, "file") == 0) { // Handle the 'file' parameter by using CURLFORM_FILE. All other params use CURLFORM_COPYCONTENTS.
    				strcpy(filePathname, commaPos + 1);
    				curl_formadd(&post, &last, CURLFORM_COPYNAME, buf, CURLFORM_FILE, commaPos + 1, CURLFORM_FILENAME, arUtilGetFileNameFromPath(commaPos + 1), CURLFORM_CONTENTTYPE, "application/octet-stream", CURLFORM_END);
    			} else {
    				curl_formadd(&post, &last, CURLFORM_COPYNAME, buf, CURLFORM_COPYCONTENTS, commaPos + 1, CURLFORM_END);
    			}
    		}

    		fclose(fp);

    		// Check that we read at least 1 form parameter.
    		if (!post) {
    			ARLOGe("Error reading CURL form data from file '%s'.\n", indexUploadPathname);
            	errorCode = -1;
    			break;
    		}

    		// Add a version to the request.
			curl_formadd(&post, &last, CURLFORM_COPYNAME, "version", CURLFORM_COPYCONTENTS, "1", CURLFORM_END);

    	    curlErr = curl_easy_setopt(curlHandle, CURLOPT_HTTPPOST, post); // Automatically sets CURLOPT_NOBODY to 0.
    		if (curlErr != CURLE_OK) {
    			ARLOGe("Error setting CURL form data: %s (%d)\n", curl_easy_strerror(curlErr), curlErr);
            	errorCode = -1;
    			break;
    		}

    		// Perform the transfer. Blocks until complete.
    	    curlErr = curl_easy_perform(curlHandle);
    	    curl_formfree(post); // Free the form resources, regardless of outcome.
    		if (curlErr != CURLE_OK) {
    			ARLOGe("Error performing CURL operation: %s (%d). %s.\n", curl_easy_strerror(curlErr), curlErr, curlErrorBuf);
    			errorCode = 2;
    			break;
    		}

    		curl_easy_getinfo (curlHandle, CURLINFO_RESPONSE_CODE, &http_response);
    		if (http_response != 200) {
    			ARLOGe("Parameter file upload failed: server returned response %ld.\n", http_response);
    			errorCode = 3;
    			break;
    		}

    		// Uploaded OK, so delete uploaded parameters file and index.
    		if (remove(indexUploadPathname) < 0) {
    			ARLOGe("Error removing index file '%s' after upload.\n", indexUploadPathname);
    			ARLOGperror(NULL);
    		}
    		if (remove(filePathname) < 0) {
    			ARLOGe("Error removing file '%s' after upload.\n", filePathname);
    			ARLOGperror(NULL);
    		}

    		uploadsDone++;
    	} // while(getNextFileInQueueWithExtension)

        pthread_mutex_lock(&(fileUploaderHandle->uploadStatusLock));

        // Set the "hide after" time.
        struct timeval time;
        gettimeofday(&time, NULL);
        fileUploaderHandle->uploadStatusHide = true;

        if (uploadsDone || errorCode) {
            if (uploadsDone) snprintf(fileUploaderHandle->uploadStatus, UPLOAD_STATUS_BUFFER_LEN, "Uploaded %d file%s", uploadsDone, (uploadsDone > 1 ? "s" : ""));
            else {
                switch (errorCode) {
                    case 1: snprintf(fileUploaderHandle->uploadStatus, UPLOAD_STATUS_BUFFER_LEN, "No Internet access. Uploads postponed."); break;
                    case 2: snprintf(fileUploaderHandle->uploadStatus, UPLOAD_STATUS_BUFFER_LEN, "Network error while uploading. Uploads postponed."); break;
                    case 3: snprintf(fileUploaderHandle->uploadStatus, UPLOAD_STATUS_BUFFER_LEN, "Server error while uploading. Uploads postponed."); break;
                    default: snprintf(fileUploaderHandle->uploadStatus, UPLOAD_STATUS_BUFFER_LEN, "Internal error while uploading. Uploads postponed."); break;
                }
            }

            // Adjust the "hide after" time.
            timeradd(&time, &(fileUploaderHandle->uploadStatusHideAfterSecs), &(fileUploaderHandle->uploadStatusHideAtTime));
        }
        pthread_mutex_unlock(&(fileUploaderHandle->uploadStatusLock));

       	ARLOGd("file uploader is DONE\n");
        threadEndSignal(threadHandle);
    }

    // Cleanup curl handle before thread exit.
	if (curlHandle) {
	    curl_easy_cleanup(curlHandle);
	    curlHandle = NULL;
	}

    free(buf);
    free(filePathname);
    free(indexUploadPathname);
    ARLOGi("End fileUploader thread.\n");
    return (NULL);
}

int fileUploaderStatusGet(FILE_UPLOAD_HANDLE_t *handle, char statusBuf[UPLOAD_STATUS_BUFFER_LEN], struct timeval *currentTime_p)
{
	int ret = 0;

	if (!handle) return (-1);

	pthread_mutex_lock(&(handle->uploadStatusLock));
	if (*(handle->uploadStatus)) {
		if (handle->uploadStatusHide && currentTime_p->tv_sec >= handle->uploadStatusHideAtTime.tv_sec && currentTime_p->tv_usec >= handle->uploadStatusHideAtTime.tv_usec) {
			*(handle->uploadStatus) = '\0';
			handle->uploadStatusHide = false;
		} else {
			strncpy(statusBuf, handle->uploadStatus, UPLOAD_STATUS_BUFFER_LEN);
			if (threadGetStatus(handle->uploadThread) == 0) ret = 1;
			else ret = 2;
		}
	}
	pthread_mutex_unlock(&(handle->uploadStatusLock));

	return (ret);
}
