#pragma once
#include "Entry.h"
extern "C" void EXPORT_API SetGeometryData(int vertex_count, int triangle_count, int corner_ids[],
	float vertex_coordinates[], bool useVertexWeight, float vertexWeights[], float texture_uv[], float texture_uv2[], float corner_colors[], float corner_normals[]);

extern "C" void EXPORT_API SetBoneData(int rootBoneIndex, int boneCount, float boneTrans[], int parentBoneID[], int vertex4BoneIndex[], float vertex4BoneWeight[]);

extern "C" void EXPORT_API RunReductionProcessing(unsigned int BorderFlagsMask, int weldDist, int tJuncDist, float max_dev, bool useVertexWeight, int targetTCount,
	float textureImprotance, float vertexColorImprotance, float edgeSetImportance, float validityImportance, float skinningImportance,
	unsigned int symmetryAxis, float symmetryOffset, bool allowDX, bool allowDegenerateTexCoords);

extern "C" void EXPORT_API GetGeometryDataSize(int &vertexCount, int &triangleCount, bool &hasUV2, bool &hasColor, bool &hasBoneWeight, bool &hasNormal);

extern "C" void EXPORT_API GetGeometryData(float *vertexData, int *triangleData, float* normalData, float *uvData, float *uv2Data, float *colorData);

extern "C" void EXPORT_API GetBoneWeightData(int *vertexBones, int *vertexBoneIDList, float *vertexBoneWeights);

spScene _scene;
spGeometryData _geom;
spRidArray _bone_ids;
bool _hasUV2 = false;
bool _hasNormal = false;
bool _hasColor = false;
bool _hasBoneWeight = false;

void Save_Geometry_To_File(spScene scene, const char *filepath);
void RunReductionProcessing(float max_dev);

