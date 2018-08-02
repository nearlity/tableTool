#include "Example.h"

void SetGeometryData(int vertex_count, int triangle_count, int corner_ids[],
	float vertex_coordinates[], bool useVertexWeight, float vertexWeights[], float texture_uv[],
	float texture_uv2[], float corner_colors[], float corner_normals[])
{
	ISimplygonSDK* sg = Entry::GetInstance()->m_pSimplygonSDK;
	_scene = sg->CreateScene();
	_geom = sg->CreateGeometryData();

	spRealArray coords = _geom->GetCoords();
	spRidArray vertex_ids = _geom->GetVertexIds();



	spRealArray vertex_weights;
	if (useVertexWeight)
	{
		_geom->AddVertexWeighting();
		vertex_weights = _geom->GetVertexWeighting();
	}

	_geom->AddTexCoords(0);
	spRealArray texcoords0 = _geom->GetTexCoords(0);

	_hasUV2 = texture_uv2 != NULL;
	spRealArray texcoords2;
	if (_hasUV2)
	{
		_geom->AddTexCoords(1);
		texcoords2 = _geom->GetTexCoords(1);
	}

	_hasNormal = corner_normals != NULL;
	spRealArray normals;
	if (_hasNormal)
	{
		_geom->AddNormals();
		normals = _geom->GetNormals();
	}

	spRealArray colors;
	_hasColor = corner_colors != NULL;
	if (_hasColor)
	{
		_geom->AddColors(0);
		colors = _geom->GetColors(0);
	}


	_geom->SetVertexCount(vertex_count);
	_geom->SetTriangleCount(triangle_count);

	for (int v = 0; v < vertex_count; ++v)
	{
		coords->SetTuple(v, &vertex_coordinates[v * 3]);
		if (useVertexWeight)
			vertex_weights->SetItem(v, vertexWeights[v]);
	}
	int corner_count = triangle_count * 3;
	for (int i = 0; i < corner_count; ++i)
	{
		vertex_ids->SetItem(i, corner_ids[i]);
		texcoords0->SetTuple(i, &texture_uv[i * 2]);
		if (_hasUV2)
		{
			texcoords2->SetTuple(i, &texture_uv2[i * 2]);
		}
		if (_hasNormal)
		{
			normals->SetTuple(i, &corner_normals[i * 3]);
		}
		if (_hasColor)
		{
			colors->SetTuple(i, &corner_colors[i * 4]);
		}
	}

	spSceneMesh mesh = sg->CreateSceneMesh();

	mesh->SetGeometry(_geom);
	mesh->SetName("mesh");
	_scene->GetRootNode()->AddChild(mesh);
	_hasBoneWeight = false;
}

void SetBoneData(int rootBoneIndex, int boneCount, float boneTrans[], int parentBoneIndex[], int vertex4BoneIndex[], float vertex4BoneWeight[])
{
	ISimplygonSDK* sg = Entry::GetInstance()->m_pSimplygonSDK;
	_geom->AddBoneWeights(4);
	spRealArray BoneWeights = _geom->GetBoneWeights();
	spRidArray BoneIds = _geom->GetBoneIds();

	spSceneBoneTable scn_bone_table = _scene->GetBoneTable();

	_bone_ids = sg->CreateRidArray();

	for (int i = 0; i < boneCount; i++)
	{
		spSceneBone bone = sg->CreateSceneBone();
		bone->SetName("bone");
		rid boneID = scn_bone_table->AddBone(bone);
		_bone_ids->AddItem(boneID);


		spTransform3 boneTransform = sg->CreateTransform3();

		//SET UP BONE IN BIND POSE
		//translate the child bone to its corrent position relative to the parent bone
		boneTransform->AddTransformation(bone->GetRelativeTransform());
		boneTransform->PreMultiply();
		boneTransform->AddTranslation(boneTrans[i * 9], boneTrans[i * 9 + 1], boneTrans[i * 9 + 2]);
		boneTransform->AddRotation(boneTrans[i * 9 + 3], 1, 0, 0);
		boneTransform->AddRotation(boneTrans[i * 9 + 4], 0, 1, 0);
		boneTransform->AddRotation(boneTrans[i * 9 + 5], 0, 0, 1);
		boneTransform->AddScaling(boneTrans[i * 9 + 6], boneTrans[i * 9 + 7], boneTrans[i * 9 + 8]);

		//store the relatvice transform
		bone->GetRelativeTransform()->DeepCopy(boneTransform->GetMatrix());
	}

	for (int i = 0; i < boneCount; i++)
	{
		rid boneID = _bone_ids->GetItem(i);
		spSceneBone bone = scn_bone_table->GetBone(boneID);
		int parentIndex = parentBoneIndex[i];
		if (parentIndex == -1)
		{
			_scene->GetRootNode()->AddChild(bone);
			continue;
		}
		if (rootBoneIndex == i)
		{
			Entry::GetInstance()->UnityLog("rootBoneIndex parent is Not -1 !!");
		}
		rid parentBoneID = _bone_ids->GetItem(parentIndex);
		spSceneBone parentBone = scn_bone_table->GetBone(parentBoneID);
		parentBone->AddChild(bone);
	}

	rid rootBoneID = _bone_ids->GetItem(rootBoneIndex);
	spSceneBone rootBone = scn_bone_table->GetBone(rootBoneID);

	int vertex_count = _geom->GetVertexCount();
	for (int i = 0; i < vertex_count; i++)
	{
		//set the bone weights to perform skining
		BoneWeights->SetItem((i * 4) + 0, vertex4BoneWeight[i * 4]);
		BoneWeights->SetItem((i * 4) + 1, vertex4BoneWeight[i * 4 + 1]);
		BoneWeights->SetItem((i * 4) + 2, vertex4BoneWeight[i * 4 + 2]);
		BoneWeights->SetItem((i * 4) + 3, vertex4BoneWeight[i * 4 + 3]);

		//set the bone ids influencing the vertex.
		BoneIds->SetItem((i * 4) + 0, _bone_ids->GetItem(vertex4BoneIndex[i * 4]));
		BoneIds->SetItem((i * 4) + 1, _bone_ids->GetItem(vertex4BoneIndex[i * 4 + 1]));
		BoneIds->SetItem((i * 4) + 2, _bone_ids->GetItem(vertex4BoneIndex[i * 4 + 2]));
		BoneIds->SetItem((i * 4) + 3, _bone_ids->GetItem(vertex4BoneIndex[i * 4 + 3]));
	}
	_scene->GetRootNode()->AddChild(rootBone);
	_hasBoneWeight = true;
}

void RunReductionProcessing(unsigned int BorderFlagsMask, int weldDist, int tJuncDist, float max_dev,
	bool useVertexWeight, int targetTCount,
	float textureImprotance, float vertexColorImprotance, float edgeSetImportance, float validityImportance, float skinningImportance,
	unsigned int symmetryAxis, float symmetryOffset, bool allowDX, bool allowDegenerateTexCoords)
{
	ISimplygonSDK* sg = Entry::GetInstance()->m_pSimplygonSDK;

	// Create the reduction processor. Set the scene that is to be processed
	spReductionProcessor red = sg->CreateReductionProcessor();
	red->SetSceneRoot(_scene->GetRootNode());

	///////////////////////////////////////////////////
	//
	// Set the Repair Settings. Current settings will mean that all visual gaps will remain in the geomtry and thus 
	// hinder the reduction on geometries that contains gaps, holes and tjunctions.
	spRepairSettings repair_settings = red->GetRepairSettings();
	// Only vertices that actually share the same position will be welded together
	//repair_settings->SetUseWelding(false);
	if (weldDist < 0)
		repair_settings->SetUseWelding(false);
	else
		repair_settings->SetWeldDist((float)weldDist);

	//_geom->Weld((float)weldDist);

	/*repair_settings->SetWeldOnlyBorderVertices(true);
	repair_settings->SetWeldOnlyObjectBoundary(true);*/

	if (tJuncDist < 0)
		repair_settings->SetUseTJunctionRemover(false);
	else
		repair_settings->SetTjuncDist((float)tJuncDist);

	//repair_settings->SetProgressivePasses(10000);

	///////////////////////////////////////////////////
	//
	// Set the Reduction Settings.
	spReductionSettings reduction_settings = red->GetReductionSettings();

	reduction_settings->SetFeatureFlags(BorderFlagsMask);
	reduction_settings->SetUseVertexWeights(useVertexWeight);
	reduction_settings->SetTextureImportance(textureImprotance);
	reduction_settings->SetUseHighQualityNormalCalculation(true);
	reduction_settings->SetVertexColorImportance(vertexColorImprotance);
	reduction_settings->SetEdgeSetImportance(edgeSetImportance);
	reduction_settings->SetValidityImportance(validityImportance);
	reduction_settings->SetSkinningImportance(skinningImportance);
	reduction_settings->SetReductionHeuristics(SG_REDUCTIONHEURISTICS_CONSISTENT);

	if (symmetryAxis >= 3)
		reduction_settings->SetUseSymmetryQuadRetriangulator(false);
	else
	{
		reduction_settings->SetUseSymmetryQuadRetriangulator(true);
		reduction_settings->SetSymmetryAxis(symmetryAxis);
		reduction_settings->SetSymmetryOffset(symmetryOffset);
	}

	//reduction_settings->SetAllowDirectX(allowDX);
	//reduction_settings->SetAllowDegenerateTexCoords(allowDegenerateTexCoords);

	// Reduce until we reach max deviation.
	reduction_settings->SetMaxDeviation(max_dev);

	if (targetTCount != -1)
	{
		reduction_settings->SetReductionRatioUsingTriangleCount(targetTCount);
	}

	///////////////////////////////////////////////////
	//
	// Set the Normal Calculation Settings.
	spNormalCalculationSettings normal_settings = red->GetNormalCalculationSettings();

	// Will completely recalculate the normals.
	/*normal_settings->SetReplaceNormals(true);
	normal_settings->SetHardEdgeAngle(90.f);*/

	// Run the process
	red->RunProcessing();
	Entry::GetInstance()->UnityLog("RunReductionProcessing End ");
}

#pragma region GetFunction
void GetGeometryDataSize(int &vertexCount, int &triangleCount, bool &hasUV2, bool &hasColor, bool &hasBoneWeight, bool &hasNormal)
{
	spGeometryData geom = Cast<ISceneMesh>(_scene->GetRootNode()->GetChild(0))->GetGeometry();
	vertexCount = geom->GetVertexCount();
	triangleCount = geom->GetTriangleCount();
	hasUV2 = _hasUV2;
	hasColor = _hasColor;
	hasBoneWeight = _hasBoneWeight;
	hasNormal = _hasNormal;
	Entry::GetInstance()->UnityLog("GetGeometryDataSize vertexCount=%d triangleCount=%d", vertexCount, triangleCount);
}

void GetGeometryData(float *vertexData, int *triangleData, float* normalData, float *uvData, float *uv2Data, float *colorData)
{
	int vertexCount = _geom->GetVertexCount();
	int triangleCount = _geom->GetTriangleCount();
	Entry::GetInstance()->UnityLog("GetGeometryData vertexCount=%d triangleCount=%d", vertexCount, triangleCount);

	spRealArray vdata = _geom->GetCoords();
	spRidArray tdata = _geom->GetVertexIds();

	spRealArray texdata = _geom->GetTexCoords(0);
	texdata->GetData(uvData);

	vdata->GetData(vertexData);
	tdata->GetData(triangleData);

	if (_hasNormal)
	{
		spRealArray ndata = _geom->GetNormals();
		ndata->GetData(normalData);
	}

	if (_hasUV2)
	{
		spRealArray tex2data = _geom->GetTexCoords(1);
		tex2data->GetData(uv2Data);
	}

	if (_hasColor)
	{
		spRealArray cdata = _geom->GetColors(0);
		cdata->GetData(colorData);
	}
}

void GetBoneWeightData(int *vertexBones, int *vertexBoneIDList, float *vertexBoneWeights)
{
	if (!_hasBoneWeight)
	{
		Entry::GetInstance()->UnityLog("Not Find BoneWeightData ");
		return;
	}
	ISimplygonSDK* sg = Entry::GetInstance()->m_pSimplygonSDK;
	int vertexCount = _geom->GetVertexCount();
	spRealArray BoneWeights = _geom->GetBoneWeights();
	spRidArray BoneIds = _geom->GetBoneIds();

	BoneIds->GetData(vertexBones);
	_bone_ids->GetData(vertexBoneIDList);
	BoneWeights->GetData(vertexBoneWeights);
}


#pragma endregion