/*
 *  calc.cpp
 *  ARToolKit6
 *
 *  This file is part of ARToolKit.
 *
 *  Copyright 2015-2017 Daqri LLC. All Rights Reserved.
 *  Copyright 2012-2015 ARToolworks, Inc. All Rights Reserved.
 *
 *  Author(s): Philip Lamb, Hirokazu Kato
 *
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

#include "calc.hpp"

#include <opencv2/calib3d/calib3d.hpp>
#include <opencv2/core/core_c.h>

static ARdouble getSizeFactor(ARdouble dist_factor[], int xsize, int ysize, int dist_function_version);
static void convParam(float intr[3][4], float dist[4], int xsize, int ysize, ARParam *param);

static void calcChessboardCorners(const Calibration::CalibrationPatternType patternType, cv::Size patternSize, float patternSpacing, std::vector<cv::Point3f>& corners)
{
    corners.resize(0);
    
    switch (patternType) {
        case Calibration::CalibrationPatternType::CHESSBOARD:
        case Calibration::CalibrationPatternType::CIRCLES_GRID:
            for (int j = 0; j < patternSize.height; j++)
                for (int i = 0; i < patternSize.width; i++)
                    corners.push_back(cv::Point3f(float(i*patternSpacing), float(j*patternSpacing), 0));
            break;
            
        case Calibration::CalibrationPatternType::ASYMMETRIC_CIRCLES_GRID:
            for (int j = 0; j < patternSize.height; j++)
                for (int i = 0; i < patternSize.width; i++)
                    corners.push_back(cv::Point3f(float((2*i + j % 2)*patternSpacing), float(j*patternSpacing), 0));
            break;
            
        default:
            ARLOGe("Unknown pattern type.\n");
    }
}

void calc(const int capturedImageNum,
          const Calibration::CalibrationPatternType patternType,
          const cv::Size patternSize,
		  const float patternSpacing,
		  const std::vector<std::vector<cv::Point2f> >& cornerSet,
		  const int width,
		  const int height,
		  ARParam *param_out,
		  ARdouble *err_min_out,
		  ARdouble *err_avg_out,
		  ARdouble *err_max_out)
{
    int i, j, k;

    // Options.
    int flags = 0;
    double aspectRatio = 1.0;
    //flags |= cv::CALIB_USE_INTRINSIC_GUESS;
    //flags |= cv::CALIB_FIX_ASPECT_RATIO;
    //flags |= cv::CALIB_FIX_PRINCIPAL_POINT;
    //flags |= cv::CALIB_ZERO_TANGENT_DIST;

    // Set up object points.
    std::vector<std::vector<cv::Point3f> > objectPoints(1);
    calcChessboardCorners(patternType, patternSize, patternSpacing, objectPoints[0]);
    objectPoints.resize(capturedImageNum, objectPoints[0]);
        
    cv::Mat intrinsics = cv::Mat::eye(3, 3, CV_64F);
    if (flags & cv::CALIB_FIX_ASPECT_RATIO)
       intrinsics.at<double>(0,0) = aspectRatio;
    
    cv::Mat distortionCoeff = cv::Mat::zeros(4, 1, CV_64F);
    std::vector<cv::Mat> rotationVectors;
    std::vector<cv::Mat> translationVectors;
    
    double rms = calibrateCamera(objectPoints, cornerSet, cv::Size(width, height), intrinsics,
                                 distortionCoeff, rotationVectors, translationVectors, flags|cv::CALIB_FIX_K3|cv::CALIB_FIX_K4|cv::CALIB_FIX_K5);
    
    ARLOGi("RMS error reported by calibrateCamera: %g\n", rms);
    
    bool ok = checkRange(intrinsics) && checkRange(distortionCoeff);
    if (!ok) ARLOGe("cv::checkRange(intrinsics) && cv::checkRange(distortionCoeff) reported not OK.\n");
    
    
    float           intr[3][4];
    float           dist[4];
    ARParam         param;

    for (j = 0; j < 3; j++) {
        for (i = 0; i < 3; i++) {
            intr[j][i] =  (float)intrinsics.at<double>(j, i);
        }
        intr[j][3] = 0.0f;
    }
    for (i = 0; i < 4; i++) {
        dist[i] = (float)distortionCoeff.at<double>(i);
    }
    convParam(intr, dist, width, height, &param);
    arParamDisp(&param);

    CvMat          *rotationVector;
    CvMat          *rotationMatrix;
    double          trans[3][4];
    ARdouble        cx, cy, cz, hx, hy, h, sx, sy, ox, oy, err;
    ARdouble        err_min = 1000000.0f, err_avg = 0.0f, err_max = 0.0f;
    rotationVector     = cvCreateMat(1, 3, CV_32FC1);
    rotationMatrix     = cvCreateMat(3, 3, CV_32FC1);

    for (k = 0; k < capturedImageNum; k++) {
        for (i = 0; i < 3; i++) {
            ((float *)(rotationVector->data.ptr))[i] = (float)rotationVectors.at(k).at<double>(i);
        }
        cvRodrigues2(rotationVector, rotationMatrix, 0);
        for (j = 0; j < 3; j++) {
            for (i = 0; i < 3; i++) {
                trans[j][i] = ((float *)(rotationMatrix->data.ptr + rotationMatrix->step*j))[i];
            }
            trans[j][3] = (float)translationVectors.at(k).at<double>(j);
        }
        //arParamDispExt(trans);

        err = 0.0;
        for (i = 0; i < patternSize.width; i++) {
            for (j = 0; j < patternSize.height; j++) {
                float x = objectPoints[0][i * patternSize.height + j].x;
                float y = objectPoints[0][i * patternSize.height + j].y;
                cx = trans[0][0] * x + trans[0][1] * y + trans[0][3];
                cy = trans[1][0] * x + trans[1][1] * y + trans[1][3];
                cz = trans[2][0] * x + trans[2][1] * y + trans[2][3];
                hx = param.mat[0][0] * cx + param.mat[0][1] * cy + param.mat[0][2] * cz + param.mat[0][3];
                hy = param.mat[1][0] * cx + param.mat[1][1] * cy + param.mat[1][2] * cz + param.mat[1][3];
                h  = param.mat[2][0] * cx + param.mat[2][1] * cy + param.mat[2][2] * cz + param.mat[2][3];
                if (h == 0.0) continue;
                sx = hx / h;
                sy = hy / h;
                arParamIdeal2Observ(param.dist_factor, sx, sy, &ox, &oy, param.dist_function_version);
                sx = (ARdouble)cornerSet[k][i * patternSize.height + j].x;
                sy = (ARdouble)cornerSet[k][i * patternSize.height + j].y;
                err += (ox - sx)*(ox - sx) + (oy - sy)*(oy - sy);
            }
        }
        err = sqrtf(err/(patternSize.width*patternSize.height));
        ARLOG("Err[%2d]: %f[pixel]\n", k + 1, err);

        // Track min, avg, and max error.
        if (err < err_min) err_min = err;
        err_avg += err;
        if (err > err_max) err_max = err;
    }
    err_avg /= (ARdouble)(capturedImageNum + 1);
    *err_min_out = err_min;
    *err_avg_out = err_avg;
    *err_max_out = err_max;

    *param_out = param;

    cvReleaseMat(&rotationVector);
    cvReleaseMat(&rotationMatrix);
}

void convParam(float intr[3][4], float dist[4], int xsize, int ysize, ARParam *param)
{
    double   s;
    int      i, j;

	param->dist_function_version = 4;
    param->xsize = xsize;
    param->ysize = ysize;

    param->dist_factor[0] = (ARdouble)dist[0];     /* k1  */
    param->dist_factor[1] = (ARdouble)dist[1];     /* k2  */
    param->dist_factor[2] = (ARdouble)dist[2];     /* p1  */
    param->dist_factor[3] = (ARdouble)dist[3];     /* p2  */
    param->dist_factor[4] = (ARdouble)intr[0][0];  /* fx  */
    param->dist_factor[5] = (ARdouble)intr[1][1];  /* fy  */
    param->dist_factor[6] = (ARdouble)intr[0][2];  /* x0  */
    param->dist_factor[7] = (ARdouble)intr[1][2];  /* y0  */
    param->dist_factor[8] = (ARdouble)1.0;         /* s   */

    for (j = 0; j < 3; j++) {
        for (i = 0; i < 4; i++) {
            param->mat[j][i] = (ARdouble)intr[j][i];
        }
    }

    s = getSizeFactor(param->dist_factor, xsize, ysize, param->dist_function_version);
    param->mat[0][0] /= s;
    param->mat[0][1] /= s;
    param->mat[1][0] /= s;
    param->mat[1][1] /= s;
    param->dist_factor[8] = s;
}

ARdouble getSizeFactor(ARdouble dist_factor[], int xsize, int ysize, int dist_function_version)
{
    ARdouble  ox, oy, ix, iy;
    ARdouble  olen, ilen;
    ARdouble  sf, sf1;

    sf = 100.0f;

    ox = 0.0f;
    oy = dist_factor[7];
    olen = dist_factor[6];
    arParamObserv2Ideal(dist_factor, ox, oy, &ix, &iy, dist_function_version);
    ilen = dist_factor[6] - ix;
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }

    ox = xsize;
    oy = dist_factor[7];
    olen = xsize - dist_factor[6];
    arParamObserv2Ideal(dist_factor, ox, oy, &ix, &iy, dist_function_version);
    ilen = ix - dist_factor[6];
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }

    ox = dist_factor[6];
    oy = 0.0;
    olen = dist_factor[7];
    arParamObserv2Ideal(dist_factor, ox, oy, &ix, &iy, dist_function_version);
    ilen = dist_factor[7] - iy;
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }

    ox = dist_factor[6];
    oy = ysize;
    olen = ysize - dist_factor[7];
    arParamObserv2Ideal(dist_factor, ox, oy, &ix, &iy, dist_function_version);
    ilen = iy - dist_factor[7];
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }


    ox = 0.0f;
    oy = 0.0f;
    arParamObserv2Ideal(dist_factor, ox, oy, &ix, &iy, dist_function_version);
    ilen = dist_factor[6] - ix;
    olen = dist_factor[6];
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }
    ilen = dist_factor[7] - iy;
    olen = dist_factor[7];
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }

    ox = xsize;
    oy = 0.0f;
    arParamObserv2Ideal(dist_factor, ox, oy, &ix, &iy, dist_function_version);
    ilen = ix - dist_factor[6];
    olen = xsize - dist_factor[6];
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }
    ilen = dist_factor[7] - iy;
    olen = dist_factor[7];
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }

    ox = 0.0f;
    oy = ysize;
    arParamObserv2Ideal(dist_factor, ox, oy, &ix, &iy, dist_function_version);
    ilen = dist_factor[6] - ix;
    olen = dist_factor[6];
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }
    ilen = iy - dist_factor[7];
    olen = ysize - dist_factor[7];
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }

    ox = xsize;
    oy = ysize;
    arParamObserv2Ideal(dist_factor, ox, oy, &ix, &iy, dist_function_version);
    ilen = ix - dist_factor[6];
    olen = xsize - dist_factor[6];
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }
    ilen = iy - dist_factor[7];
    olen = ysize - dist_factor[7];
    //ARLOG("Olen = %f, Ilen = %f, s = %f\n", olen, ilen, ilen / olen);
    if (ilen > 0.0f) {
        sf1 = ilen / olen;
        if (sf1 < sf) sf = sf1;
    }

    if (sf == 100.0f) sf = 1.0f;

    return sf;
}

