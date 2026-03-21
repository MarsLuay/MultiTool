/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

#include "stdafx.h"
#include "CrystalMark.h"
#include "CrystalMarkDlg.h"
#include "AboutDlg.h"

IMPLEMENT_DYNCREATE(CAboutDlg, CDialog)

CAboutDlg::CAboutDlg(CWnd* pParent /*=NULL*/)
	: CDialogFx(CAboutDlg::IDD, pParent)
{
	CMainDialogFx* p = (CMainDialogFx*)pParent;

	m_SizeX = SIZE_X;
	m_SizeY = SIZE_Y;

	m_ZoomType = p->GetZoomType();
	m_FontScale = p->GetFontScale();
	m_FontRatio = p->GetFontRatio();
	m_FontFace = p->GetFontFace();
	m_FontRender = p->GetFontRender();
	m_CurrentLangPath = p->GetCurrentLangPath();
	m_DefaultLangPath = p->GetDefaultLangPath();
	m_ThemeDir = p->GetThemeDir();
	m_CurrentTheme = p->GetCurrentTheme();
	m_DefaultTheme = p->GetDefaultTheme();
	m_Ini = p->GetIniPath();

#ifdef CRYSTALMARK_3D
	m_BackgroundName = _T("About");
#elif SUISHO_SHIZUKU_SUPPORT
	m_BackgroundName = _T("About");
#else
	m_BackgroundName = _T("");
#endif
}

CAboutDlg::~CAboutDlg()
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogFx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_LOGO, m_CtrlLogo);
#ifdef SUISHO_SHIZUKU_SUPPORT
	DDX_Control(pDX, IDC_SECRET_VOICE, m_CtrlSecretVoice);
#endif
	DDX_Control(pDX, IDC_PROJECT_SITE_1, m_CtrlProjectSite1);
	DDX_Control(pDX, IDC_PROJECT_SITE_2, m_CtrlProjectSite2);
	DDX_Control(pDX, IDC_PROJECT_SITE_3, m_CtrlProjectSite3);
	DDX_Control(pDX, IDC_PROJECT_SITE_4, m_CtrlProjectSite4);
	DDX_Control(pDX, IDC_PROJECT_SITE_5, m_CtrlProjectSite5);
#ifdef CRYSTALMARK_3D
	DDX_Control(pDX, IDC_PROJECT_OWNER_1, m_CtrlProjectOwner1);
	DDX_Control(pDX, IDC_PROJECT_OWNER_2, m_CtrlProjectOwner2);
	DDX_Control(pDX, IDC_PROJECT_OWNER_3, m_CtrlProjectOwner3);
	DDX_Control(pDX, IDC_PROJECT_OWNER_4, m_CtrlProjectOwner4);
	DDX_Control(pDX, IDC_PROJECT_OWNER_5, m_CtrlProjectOwner5);
#endif
	DDX_Control(pDX, IDC_VERSION, m_CtrlVersion);
	DDX_Control(pDX, IDC_LICENSE, m_CtrlLicense);	
	DDX_Control(pDX, IDC_RELEASE, m_CtrlRelease);
	DDX_Control(pDX, IDC_COPYRIGHT1, m_CtrlCopyright1);
	DDX_Control(pDX, IDC_COPYRIGHT2, m_CtrlCopyright2);
	DDX_Control(pDX, IDC_COPYRIGHT3, m_CtrlCopyright3);
	DDX_Control(pDX, IDC_EDITION, m_CtrlEdition);
}

BOOL CAboutDlg::OnInitDialog()
{
	CDialogFx::OnInitDialog();

	SetWindowText(i18n(_T("WindowTitle"), _T("ABOUT")));

	m_bShowWindow = TRUE;
	m_CtrlVersion.SetWindowText(PRODUCT_NAME _T(" ") PRODUCT_VERSION);
	m_CtrlEdition.SetWindowText(PRODUCT_EDITION);
	m_CtrlRelease.SetWindowText(_T("Release: ") PRODUCT_RELEASE);
	m_CtrlCopyright1.SetWindowText(PRODUCT_COPYRIGHT_1);
	m_CtrlCopyright2.SetWindowText(PRODUCT_COPYRIGHT_2);
	m_CtrlCopyright3.SetWindowText(PRODUCT_COPYRIGHT_3);
	m_CtrlLicense.SetWindowText(PRODUCT_LICENSE);

	UpdateDialogSize();

	CenterWindow();
	ShowWindow(SW_SHOW);
	return TRUE;
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialogFx)
	ON_BN_CLICKED(IDC_LOGO, &CAboutDlg::OnLogo)
	ON_BN_CLICKED(IDC_LICENSE, &CAboutDlg::OnLicense)
	ON_BN_CLICKED(IDC_VERSION, &CAboutDlg::OnVersion)
#ifdef SUISHO_SHIZUKU_SUPPORT
	ON_BN_CLICKED(IDC_SECRET_VOICE, &CAboutDlg::OnSecretVoice)
	ON_BN_CLICKED(IDC_PROJECT_SITE_1, &CAboutDlg::OnProjectSite1)
	ON_BN_CLICKED(IDC_PROJECT_SITE_2, &CAboutDlg::OnProjectSite2)
	ON_BN_CLICKED(IDC_PROJECT_SITE_3, &CAboutDlg::OnProjectSite3)
	ON_BN_CLICKED(IDC_PROJECT_SITE_4, &CAboutDlg::OnProjectSite4)
	ON_BN_CLICKED(IDC_PROJECT_SITE_5, &CAboutDlg::OnProjectSite5)
#endif

#ifdef CRYSTALMARK_3D
	ON_BN_CLICKED(IDC_PROJECT_SITE_1, &CAboutDlg::OnProjectSite1)
	ON_BN_CLICKED(IDC_PROJECT_SITE_2, &CAboutDlg::OnProjectSite2)
	ON_BN_CLICKED(IDC_PROJECT_SITE_3, &CAboutDlg::OnProjectSite3)
	ON_BN_CLICKED(IDC_PROJECT_SITE_4, &CAboutDlg::OnProjectSite4)
	ON_BN_CLICKED(IDC_PROJECT_SITE_5, &CAboutDlg::OnProjectSite5)
	ON_BN_CLICKED(IDC_PROJECT_OWNER_1, &CAboutDlg::OnProjectOwner1)
	ON_BN_CLICKED(IDC_PROJECT_OWNER_2, &CAboutDlg::OnProjectOwner2)
	ON_BN_CLICKED(IDC_PROJECT_OWNER_3, &CAboutDlg::OnProjectOwner3)
	ON_BN_CLICKED(IDC_PROJECT_OWNER_4, &CAboutDlg::OnProjectOwner4)
	ON_BN_CLICKED(IDC_PROJECT_OWNER_5, &CAboutDlg::OnProjectOwner5)
#endif
END_MESSAGE_MAP()


void CAboutDlg::UpdateDialogSize()
{
	CDialogFx::UpdateDialogSize();
	m_bHighContrast = FALSE;

	ChangeZoomType(m_ZoomType);
	SetClientSize(m_SizeX, m_SizeY, m_ZoomRatio);
	UpdateBackground(TRUE, m_bDarkMode);


#ifdef CRYSTALMARK_3D
	m_CtrlLogo.InitControl(416, 8, 128, 128, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Logo")), 1, BS_CENTER, OwnerDrawImage, FALSE, FALSE, FALSE);
	m_CtrlProjectSite1.InitControl(8, 208, 300, 192, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite2.InitControl(332, 208, 300, 192, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite3.InitControl(8, 424, 300, 192, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite4.InitControl(332, 424, 300, 192, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite5.InitControl(332, 164, 300, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectOwner1.InitControl(8, 400, 300, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectOwner2.InitControl(332, 400, 300, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectOwner3.InitControl(8, 616, 300, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectOwner4.InitControl(332, 616, 300, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectOwner5.InitControl(332, 180, 300, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);

	m_CtrlProjectSite1.SetHandCursor();
	m_CtrlProjectSite2.SetHandCursor();
	m_CtrlProjectSite3.SetHandCursor();
	m_CtrlProjectSite4.SetHandCursor();
	m_CtrlProjectSite5.SetHandCursor();
	m_CtrlProjectOwner1.SetHandCursor();
	m_CtrlProjectOwner2.SetHandCursor();
	m_CtrlProjectOwner3.SetHandCursor();
	m_CtrlProjectOwner4.SetHandCursor();
	m_CtrlProjectOwner5.SetHandCursor();

//	m_CtrlLogo.ShowWindow(SW_HIDE);

#elif SUISHO_AOI_SUPPORT
//	m_CtrlLogo.InitControl(32, 484, 128, 144, m_ZoomRatio, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlLogo.InitControl(80, 12, 128, 128, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Logo")), 1, BS_CENTER, OwnerDrawImage, FALSE, FALSE, FALSE);
	m_CtrlSecretVoice.InitControl(392, 288, 60, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite1.InitControl(184, 508, 148, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite2.InitControl(244, 540, 108, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite3.InitControl(232, 556, 180, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite4.InitControl(244, 576, 120, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite5.InitControl(0, 0, 0, 0, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlSecretVoice.SetHandCursor();
	m_CtrlProjectSite1.SetHandCursor();
	m_CtrlProjectSite2.SetHandCursor();
	m_CtrlProjectSite3.SetHandCursor();
	m_CtrlProjectSite4.SetHandCursor();
	m_CtrlProjectSite5.SetHandCursor();

#elif SUISHO_SHIZUKU_SUPPORT
	m_CtrlLogo.InitControl(80, 12, 128, 128, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Logo")), 1, BS_CENTER, OwnerDrawImage, FALSE, FALSE, FALSE);
	m_CtrlSecretVoice.InitControl(364, 264, 44, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite1.InitControl(64, 372, 140, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite2.InitControl(64, 416, 148, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite3.InitControl(64, 432, 184, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite4.InitControl(40, 460, 208, 16, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlProjectSite5.InitControl(92, 504, 432, 124, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlSecretVoice.SetHandCursor();
	m_CtrlProjectSite1.SetHandCursor();
	m_CtrlProjectSite2.SetHandCursor();
	m_CtrlProjectSite3.SetHandCursor();
	m_CtrlProjectSite4.SetHandCursor();
	m_CtrlProjectSite5.SetHandCursor();

#else
	m_CtrlLogo.InitControl(20, 20, 128, 128, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Logo")), 1, BS_CENTER, OwnerDrawImage, FALSE, FALSE, FALSE);
	m_CtrlProjectSite1.ShowWindow(SW_HIDE);
	m_CtrlProjectSite2.ShowWindow(SW_HIDE);
	m_CtrlProjectSite3.ShowWindow(SW_HIDE);
	m_CtrlProjectSite4.ShowWindow(SW_HIDE);
	m_CtrlProjectSite5.ShowWindow(SW_HIDE);
#endif

	m_CtrlLogo.SetHandCursor();
	m_CtrlVersion.SetHandCursor();
	m_CtrlLicense.SetHandCursor();

#ifdef CRYSTALMARK_3D
	m_CtrlVersion.SetFontEx(m_FontFace, 20, 20, m_ZoomRatio, m_FontRatio, RGB(255, 255, 255), FW_BOLD, m_FontRender);
	m_CtrlEdition.SetFontEx(m_FontFace, 20, 20, m_ZoomRatio, m_FontRatio, RGB(255, 255, 255), FW_BOLD, m_FontRender);
	m_CtrlRelease.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, RGB(255, 255, 255), FW_NORMAL, m_FontRender);
	m_CtrlCopyright1.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, RGB(255, 255, 255), FW_NORMAL, m_FontRender);
	m_CtrlCopyright2.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, RGB(255, 255, 255), FW_NORMAL, m_FontRender);
	m_CtrlCopyright3.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, RGB(255, 255, 255), FW_NORMAL, m_FontRender);
	m_CtrlLicense.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, RGB(255, 255, 255), FW_NORMAL, m_FontRender);
#else
	m_CtrlVersion.SetFontEx(m_FontFace, 20, 20, m_ZoomRatio, m_FontRatio, RGB(0, 0, 0), FW_BOLD, m_FontRender);
	m_CtrlEdition.SetFontEx(m_FontFace, 20, 20, m_ZoomRatio, m_FontRatio, RGB(0, 0, 0), FW_BOLD, m_FontRender);
	m_CtrlRelease.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, RGB(0, 0, 0), FW_NORMAL, m_FontRender);
	m_CtrlCopyright1.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, RGB(0, 0, 0), FW_NORMAL, m_FontRender);
	m_CtrlCopyright2.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, RGB(0, 0, 0), FW_NORMAL, m_FontRender);
	m_CtrlCopyright3.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, RGB(0, 0, 0), FW_NORMAL, m_FontRender);
	m_CtrlLicense.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, RGB(0, 0, 0), FW_NORMAL, m_FontRender);
#endif

#ifdef CRYSTALMARK_3D
	m_CtrlVersion.InitControl(12, 32, 300, 28, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlEdition.InitControl(12, 60, 300, 28, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlRelease.InitControl(12, 116, 300, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlCopyright1.InitControl(12, 136, 300, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlCopyright2.InitControl(12, 156, 300, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlCopyright3.InitControl(12, 176, 300, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlLicense.InitControl(12, 176, 300, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlCopyright3.ShowWindow(SW_HIDE);
#elif SUISHO_AOI_SUPPORT
	m_CtrlVersion.InitControl(0, 152, 288, 28, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlEdition.InitControl(0, 180, 288, 28, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlRelease.InitControl(0, 216, 288, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlCopyright1.InitControl(0, 236, 288, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlCopyright2.InitControl(0, 256, 288, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlCopyright3.InitControl(0, 276, 288, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlLicense.InitControl(0, 296, 288, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);

#elif SUISHO_SHIZUKU_SUPPORT
	m_CtrlVersion.InitControl(0, 152, 288, 28, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlEdition.InitControl(0, 180, 288, 28, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlRelease.InitControl(0, 216, 288, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlCopyright1.InitControl(0, 236, 288, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlCopyright2.InitControl(0, 256, 288, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlCopyright3.InitControl(0, 276, 288, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);
	m_CtrlLicense.InitControl(0, 296, 288, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, FALSE, FALSE, FALSE);

#else
	m_CtrlVersion.InitControl(160, 12, 364, 28, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlEdition.InitControl(160, 40, 364, 28, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlRelease.InitControl(160, 76, 364, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlCopyright1.InitControl(160, 96, 364, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlCopyright2.InitControl(160, 116, 364, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlCopyright3.InitControl(160, 136, 364, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlLicense.InitControl(160, 136, 364, 20, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_CENTER, OwnerDrawTransparent, m_bHighContrast, m_bDarkMode, FALSE);
	m_CtrlCopyright3.ShowWindow(SW_HIDE);
#endif

	Invalidate();
}

void CAboutDlg::OnLogo()
{
	if (GetUserDefaultLCID() == 0x0411)// Japanese
	{
		OpenUrl(URL_MAIN_JA);
	}
	else // Other Language
	{
		OpenUrl(URL_MAIN_EN);
	}
}

void CAboutDlg::OnVersion()
{
	if (GetUserDefaultLCID() == 0x0411)// Japanese
	{
		OpenUrl(URL_VERSION_JA);
	}
	else // Other Language
	{
		OpenUrl(URL_VERSION_EN);
	}

}
void CAboutDlg::OnLicense()
{
	if (GetUserDefaultLCID() == 0x0411)// Japanese
	{
		OpenUrl(URL_LICENSE_JA);
	}
	else // Other Language
	{
		OpenUrl(URL_LICENSE_EN);
	}
}

#ifdef SUISHO_SHIZUKU_SUPPORT
void CAboutDlg::OnSecretVoice()
{
	::PostMessageW(m_ParentWnd->GetSafeHwnd(), WM_SECRET_VOICE, 0, 0);
}
void CAboutDlg::OnProjectSite1() { OpenUrl(URL_PROJECT_SITE_1); }
void CAboutDlg::OnProjectSite2() { OpenUrl(URL_PROJECT_SITE_2); }
void CAboutDlg::OnProjectSite3() { OpenUrl(URL_PROJECT_SITE_3); }
void CAboutDlg::OnProjectSite4() { OpenUrl(URL_PROJECT_SITE_4); }
void CAboutDlg::OnProjectSite5() { OpenUrl(URL_PROJECT_SITE_5); }
#endif

#ifdef CRYSTALMARK_3D
void CAboutDlg::OnProjectSite1(){  OpenUrl(URL_PROJECT_SITE_1); }
void CAboutDlg::OnProjectSite2(){  OpenUrl(URL_PROJECT_SITE_2); }
void CAboutDlg::OnProjectSite3(){  OpenUrl(URL_PROJECT_SITE_3); }
void CAboutDlg::OnProjectSite4(){  OpenUrl(URL_PROJECT_SITE_4); }
void CAboutDlg::OnProjectSite5(){  OpenUrl(URL_PROJECT_SITE_5); }
void CAboutDlg::OnProjectOwner1(){ OpenUrl(URL_PROJECT_OWNER_1); }
void CAboutDlg::OnProjectOwner2(){ OpenUrl(URL_PROJECT_OWNER_2); }
void CAboutDlg::OnProjectOwner3(){ OpenUrl(URL_PROJECT_OWNER_3); }
void CAboutDlg::OnProjectOwner4(){ OpenUrl(URL_PROJECT_OWNER_4); }
void CAboutDlg::OnProjectOwner5(){ OpenUrl(URL_PROJECT_OWNER_5); }
#endif