#pragma once
#include <stdio.h>
#include <string>
#include <stdlib.h>
#include <io.h>
#include <windows.h>
#include <tchar.h>
#include "SimplygonSDK.h"
using namespace std;
using namespace SimplygonSDK;

#define  EXPORT_API __declspec(dllexport)

typedef void(__stdcall * CPPCallback)(const char* chLog);
extern "C" void EXPORT_API SetLogCallback(CPPCallback callback);
extern "C" void EXPORT_API InitSDK(const char* chDllPath);
extern "C" void EXPORT_API DeInitSDK();

typedef int (CALLBACK* LPINITIALIZESIMPLYGONSDK)(LPCTSTR license_data, SimplygonSDK::ISimplygonSDK **pInterfacePtr);
typedef void (CALLBACK* LPDEINITIALIZESIMPLYGONSDK)();
typedef void (CALLBACK* LPGETINTERFACEVERSIONSIMPLYGONSDK)(char *deststring);
typedef int (CALLBACK* LPPOLLLOGSIMPLYGONSDK)(char *destbuffer, int max_length);

class Entry
{
private:
	Entry();
	~Entry();
private:
	static Entry* m_pInstance;
	HINSTANCE m_hSDKDLL;
	LPINITIALIZESIMPLYGONSDK m_ptrInitializeSimplygonSDK;
	LPDEINITIALIZESIMPLYGONSDK m_ptrDeinitializeSimplygonSDK;
	LPGETINTERFACEVERSIONSIMPLYGONSDK m_ptrGetInterfaceVersionSimplygonSDK;
	LPPOLLLOGSIMPLYGONSDK m_ptrPollLogSimplygonSDK;
public:
	ISimplygonSDK* m_pSimplygonSDK;
public:
	static Entry* GetInstance();
	CPPCallback m_pLogCallback;
	bool LoadSimplygonSDKDLL(const char* chDllPath);
	bool InitSimplygonSDK();
	void DeinitSimplygonSDK();
	void UnityLog(const char* format, ...);
	int PollLog(LPTSTR dest, int max_len_dest);
};