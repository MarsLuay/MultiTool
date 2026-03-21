/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

#pragma once

#ifndef _SECURE_ATL
#define _SECURE_ATL 1
#endif

#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN
#endif

#ifndef WINVER
#define WINVER 0x0501
#endif

#ifndef _WIN32_WINNT              
#define _WIN32_WINNT 0x0501
#endif						

#ifndef _WIN32_WINDOWS
#define _WIN32_WINDOWS 0x0410
#endif

#ifndef _WIN32_IE
#define _WIN32_IE 0x0600
#endif

#ifndef CLEARTYPE_NATURAL_QUALITY
#define CLEARTYPE_NATURAL_QUALITY 6
#endif

#ifndef ANTIALIASED_QUALITY
#define ANTIALIASED_QUALITY 4
#endif

#define _AFX_NO_MFC_CONTROLS_IN_DIALOGS
#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS
#define _AFX_ALL_WARNINGS

#include <afxwin.h>				// MFC core and standard component
#include <afxext.h>				// Extended MFC
#include <afxdtctl.h>			// MFC IE4 Common Control support
#include <afxcmn.h>				// MFC Windows Common Control support

#include "CrystalMark.h"
#include "CrystalMarkDlg.h"

#ifdef CRYSTALMARK_RETRO
#include "..\CrystalMarkRetro.h"
#include "..\CrystalMarkRetroDlg.h"
#define MAIN_DIALOG_NAME CCrystalMarkRetroDlg
#endif

#ifdef CRYSTALMARK_3D
#include "..\CrystalMark3D.h"
#include "..\CrystalMark3DDlg.h"
#define MAIN_DIALOG_NAME CCrystalMark3DDlg
#endif

#if _MSC_VER > 1310
#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif

#if _MSC_VER > 1310
	#ifdef SUISHO_AOI_SUPPORT
		#ifdef _M_ARM
		#define PRODUCT_EDITION			_T("Aoi Edition ARM32")
		#elif _M_ARM64
		#define PRODUCT_EDITION			_T("Aoi Edition ARM64")
		#elif _M_X64
		#define PRODUCT_EDITION			_T("Aoi Edition x64")
		#else
		#define PRODUCT_EDITION			_T("Aoi Edition x86")
		#endif

	#elif SUISHO_SHIZUKU_SUPPORT
		#ifdef _M_ARM
		#define PRODUCT_EDITION			_T("Shizuku Edition ARM32")
		#elif _M_ARM64
		#define PRODUCT_EDITION			_T("Shizuku Edition ARM64")
		#elif _M_X64
		#define PRODUCT_EDITION			_T("Shizuku Edition x64")
		#else
		#define PRODUCT_EDITION			_T("Shizuku Edition x86")
		#endif

	#else
		#ifdef _M_ARM
		#define PRODUCT_EDITION			_T("ARM32")
		#elif _M_ARM64
		#define PRODUCT_EDITION			_T("ARM64")
		#elif _M_X64
		#define PRODUCT_EDITION			_T("x64")
		#else
		#define PRODUCT_EDITION			_T("x86")
		#endif
	#endif
#else
	#ifdef SUISHO_AOI_SUPPORT
		#ifdef UNICODE
		#define PRODUCT_EDITION			_T("Aoi Edition for NT")
		#else
		#define PRODUCT_EDITION			_T("Aoi Edition for 9x")
		#endif

	#elif SUISHO_SHIZUKU_SUPPORT
		#ifdef UNICODE
		#define PRODUCT_EDITION			_T("Shizuku Edition for NT")
		#else
		#define PRODUCT_EDITION			_T("Shizuku Edition for 9x")
		#endif

	#else
		#ifdef UNICODE
		#define PRODUCT_EDITION			_T("for NT")
		#else
		#define PRODUCT_EDITION			_T("for 9x")
		#endif
	#endif
#endif


#if _MSC_VER > 1310
#define DEFAULT_FONT_FACE_1			_T("Segoe UI")
#define DEFAULT_FONT_FACE_2			_T("Tahoma")
#else
#define DEFAULT_FONT_FACE_1			_T("Tahoma")
#define DEFAULT_FONT_FACE_2			_T("Arial")
#endif

#define THEME_DIR					_T("Resource\\Theme\\")
#define LANGUAGE_DIR				_T("Resource\\Language\\")
#define VOICE_DIR					_T("Resource\\Voice\\")

#define MENU_THEME_INDEX			1
#define MENU_LANG_INDEX				3

#define DEFAULT_THEME				_T("Default")
#define DEFAULT_LANGUAGE			_T("English")

#define TIMER_UPDATE_DIALOG			500

#define WM_UPDATE_SCORE				(WM_APP+0x1001)
#define WM_UPDATE_MESSAGE			(WM_APP+0x1002)
#define WM_EXIT_BENCHMARK			(WM_APP+0x1003)
#define WM_START_BENCHMARK			(WM_APP+0x1004)
#define WM_SECRET_VOICE				(WM_APP+0x1005)

