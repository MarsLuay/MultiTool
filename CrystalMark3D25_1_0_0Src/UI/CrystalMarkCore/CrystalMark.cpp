/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

#include "stdafx.h"
#include "CrystalMark.h"
#include "CrystalMarkDlg.h"
#include <afxole.h>

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

BEGIN_MESSAGE_MAP(CCrystalMarkApp, CWinApp)
END_MESSAGE_MAP()

CCrystalMarkApp theApp;

CCrystalMarkApp::CCrystalMarkApp()
{
}

BOOL CCrystalMarkApp::InitInstance()
{
	BOOL flagAfxOleInit = FALSE;
	INITCOMMONCONTROLSEX InitCtrls = {0};
	InitCtrls.dwSize = sizeof(InitCtrls);
	InitCtrls.dwICC = ICC_WIN95_CLASSES;
#if _MSC_VER > 1310
	InitCommonControlsEx(&InitCtrls);
#else
	InitCommonControls();
#endif

	CWinApp::InitInstance();

#ifdef UNICODE
	if (AfxOleInit())
	{
		flagAfxOleInit = TRUE;
		AfxOleGetMessageFilter()->SetMessagePendingDelay(60 * 1000);
		AfxOleGetMessageFilter()->EnableNotRespondingDialog(FALSE);
		AfxOleGetMessageFilter()->EnableBusyDialog(FALSE);
	}
	else
	{
		typedef BOOL(WINAPI* FuncCoInitializeEx)(LPVOID, DWORD);
		FuncCoInitializeEx pCoInitializeEx = NULL;
		HMODULE hModule = GetModuleHandle(_T("ole32.dll"));
		if (hModule)
		{
			pCoInitializeEx = (FuncCoInitializeEx)GetProcAddress(hModule, "CoInitializeEx");

		}
		if (pCoInitializeEx != NULL)
		{
			pCoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
		}
		else
		{
			(void)CoInitialize(NULL);
		}
	}

	typedef BOOL(WINAPI* FuncCoInitializeSecurity)(PSECURITY_DESCRIPTOR, LONG, SOLE_AUTHENTICATION_SERVICE*, void*, DWORD, DWORD, void*, DWORD, void*);
	FuncCoInitializeSecurity pCoInitializeSecurity = NULL;
	HMODULE hModule = GetModuleHandle(_T("ole32.dll"));
	if (hModule)
	{
		pCoInitializeSecurity = (FuncCoInitializeSecurity)GetProcAddress(hModule, "CoInitializeSecurity");
	}
	if (pCoInitializeSecurity != NULL)
	{
		pCoInitializeSecurity(NULL, -1, NULL, NULL, RPC_C_AUTHN_LEVEL_DEFAULT, RPC_C_IMP_LEVEL_IMPERSONATE, NULL, EOAC_NONE, NULL);
	}
#else
	CoInitialize(NULL);
#endif

	MAIN_DIALOG_NAME* dlg = new MAIN_DIALOG_NAME;
	m_pMainWnd = dlg;
	dlg->DoModal();
	delete dlg;

#ifdef UNICODE
	if (flagAfxOleInit)
	{
		AfxOleTerm(FALSE);
	}
	else
	{
		CoUninitialize();
	}
#else
	CoUninitialize();
#endif

	return FALSE;
}