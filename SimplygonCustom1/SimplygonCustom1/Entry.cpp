#include "Entry.h"

Entry::Entry()
{
}


Entry::~Entry()
{
	if (Entry::m_pInstance)
		delete Entry::m_pInstance;
}

Entry* Entry::m_pInstance = new Entry();
Entry* Entry::GetInstance()
{
	return m_pInstance;
}

void Entry::UnityLog(const char* format, ...)
{
	char buff[256];
	va_list args;
	va_start(args, format);
	vsprintf_s(buff, format, args);
	va_end(args);
	m_pLogCallback(buff);
}

bool Entry::LoadSimplygonSDKDLL(const char* chDllPath)
{
	WCHAR wsDllPath[MAX_PATH] = { 0 };
	MultiByteToWideChar(CP_ACP, 0, chDllPath, -1, wsDllPath, sizeof(wsDllPath));
	m_hSDKDLL = LoadLibrary(wsDllPath);
	if (m_hSDKDLL == NULL)
	{
		m_pLogCallback("Load Simplygon SDK dll failed!!");
		return false;
	}

	m_ptrInitializeSimplygonSDK = (LPINITIALIZESIMPLYGONSDK)GetProcAddress(m_hSDKDLL, "InitializeSimplygonSDK");
	if (m_ptrInitializeSimplygonSDK == NULL)
	{
		m_pLogCallback("GetProcAddress InitializeSimplygonSDK failed!!");
		return false;
	}
	m_ptrDeinitializeSimplygonSDK = (LPDEINITIALIZESIMPLYGONSDK)GetProcAddress(m_hSDKDLL, "DeinitializeSimplygonSDK");
	if (m_ptrDeinitializeSimplygonSDK == NULL)
	{
		m_pLogCallback("GetProcAddress DeinitializeSimplygonSDK failed!!");
		return false;
	}
	m_ptrGetInterfaceVersionSimplygonSDK = (LPGETINTERFACEVERSIONSIMPLYGONSDK)GetProcAddress(m_hSDKDLL, "GetInterfaceVersionSimplygonSDK");
	if (m_ptrGetInterfaceVersionSimplygonSDK == NULL)
	{
		m_pLogCallback("GetProcAddress GetInterfaceVersionSimplygonSDK failed!!");
		return false;
	}
	m_ptrPollLogSimplygonSDK = (LPPOLLLOGSIMPLYGONSDK)GetProcAddress(m_hSDKDLL, "PollLogSimplygonSDK");
	if (m_ptrPollLogSimplygonSDK == NULL)
	{
		m_pLogCallback("GetProcAddress PollLogSimplygonSDK failed!!");
		return false;
	}

	m_pLogCallback("Load Simplygon SDK dll success!!");
	return true;
}

bool Entry::InitSimplygonSDK()
{
	LPTSTR licenseData = _T("");
	int retCode = m_ptrInitializeSimplygonSDK(licenseData, &m_pSimplygonSDK);
	UnityLog("InitSimplygonSDK RetCode=%d", retCode);

	if (retCode == 0)
	{
		const char* version = m_pSimplygonSDK->GetVersion();
		UnityLog("Simplygon initialized and running. Version: %s", version);
	}
	return true;
}

void Entry::DeinitSimplygonSDK()
{
	if (m_hSDKDLL == NULL)
	{
		m_pLogCallback("DeinitSimplygonSDK failed m_hSDKDLL is NULL !");
		return;
	}

	if (m_ptrDeinitializeSimplygonSDK == NULL)
	{
		m_pLogCallback("DeinitSimplygonSDK failed Func is NULL !");
		return;
	}

	m_ptrDeinitializeSimplygonSDK();

	FreeLibrary(m_hSDKDLL);
	m_hSDKDLL = NULL;
	m_ptrInitializeSimplygonSDK = NULL;
	m_ptrDeinitializeSimplygonSDK = NULL;
	m_ptrGetInterfaceVersionSimplygonSDK = NULL;
	m_ptrPollLogSimplygonSDK = NULL;
}

void SetLogCallback(CPPCallback callback)
{
	Entry::GetInstance()->m_pLogCallback = callback;
	Entry::GetInstance()->m_pLogCallback("Hello World!!!");
}

void InitSDK(const char* chDllPath)
{
	bool ret = Entry::GetInstance()->LoadSimplygonSDKDLL(chDllPath);
	if (!ret)
		return;
	Entry::GetInstance()->InitSimplygonSDK();
}

void DeInitSDK()
{
	Entry::GetInstance()->DeinitSimplygonSDK();
}

int Entry::PollLog(LPTSTR dest, int max_len_dest)
{
	if (dest == nullptr || max_len_dest == 0 || m_ptrPollLogSimplygonSDK == nullptr)
	{
		return 0;
	}

	int sz;

#ifdef UNICODE
	char *tmp = new char[max_len_dest];
	m_ptrPollLogSimplygonSDK(tmp, max_len_dest);
	size_t cnv_sz;
	mbstowcs_s(
		&cnv_sz,
		dest,
		max_len_dest,
		tmp,
		_TRUNCATE
		);
	delete[] tmp;
	sz = (int)cnv_sz;
#else
	sz = PollLogSimplygonSDKPtr(dest, max_len_dest);
#endif//UNICODE	

	return sz;
}