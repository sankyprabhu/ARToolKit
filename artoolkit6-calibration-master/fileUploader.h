/*
 *  fileUploader.h
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


#ifndef FILEUPLOADER_H
#define FILEUPLOADER_H

//
// HTML form and file uploader via HTTP POST.
//
// When tickled, each index file in "queueDirPath" with extension "formExtension" will be opened
// and read for form data to be uploaded to URL "formPostURL" via HTTP POST.
// The format of the index file is 1 form field per line. From the beginning of the line up to
// the first ',' character is taken as the field name. The rest of the line after the ','
// up to the end-of-line is taken as the field contents.
// A field with the name 'file' is treated differently. If such a field is found, the field
// contents are taken as the pathname to a file to be uploaded. The file will be uploaded
// under a field named 'file', with its filename (not including any other path component)
// supplied as the filename portion of the field.
//
// Uses libcURL internally.
// Don't forget to add library load calls on the Java side:
//    static {
//    	System.loadLibrary("crypto");
//    	System.loadLibrary("ssl");
//    	System.loadLibrary("curl");
//    }
//

#include <sys/time.h> // struct timeval, gettimeofday(), timeradd()
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

#define UPLOAD_STATUS_BUFFER_LEN 128

// Check for existence of queue directory, and create if not already existing.
// Returns false if directory could not be created, true otherwise.
// This needs to be done no later than before the first call to fileUploaderTickle().
bool fileUploaderCreateQueueDir(const char *queueDirPath);
    
typedef struct _FILE_UPLOAD_HANDLE FILE_UPLOAD_HANDLE_t;

FILE_UPLOAD_HANDLE_t *fileUploaderInit(const char *queueDirPath, const char *formExtension, const char *formPostURL, const float statusHideAfterSecs);

void fileUploaderFinal(FILE_UPLOAD_HANDLE_t **handle_p);

bool fileUploaderTickle(FILE_UPLOAD_HANDLE_t *handle);

// -1 = An error.
// 0 = no background tasks or messages.
// 1 = background task currently in progress.
// 2 = background task complete, message still to be shown.
int fileUploaderStatusGet(FILE_UPLOAD_HANDLE_t *handle, char statusBuf[UPLOAD_STATUS_BUFFER_LEN], struct timeval *currentTime_p);

#ifdef __cplusplus
}
#endif
#endif // !FILEUPLOADER_H
