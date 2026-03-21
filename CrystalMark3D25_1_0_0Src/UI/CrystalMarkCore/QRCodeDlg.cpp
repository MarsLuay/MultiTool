/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

#include "stdafx.h"
#include "CrystalMark.h"
#include "CrystalMarkDlg.h"
#include "QRCodeDlg.h"

IMPLEMENT_DYNCREATE(CQRCodeDlg, CDialog)

CQRCodeDlg::CQRCodeDlg(CWnd* pParent /*=NULL*/)
	: CDialogFx(CQRCodeDlg::IDD, pParent)
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

	m_BackgroundName = _T("");
	m_Background = RGB(0xFF, 0xFF, 0xFF);

	m_QRCodePath = ((CCrystalMarkDlg*)p)->GetQRCodePath();
}

CQRCodeDlg::~CQRCodeDlg()
{
}

void CQRCodeDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogFx::DoDataExchange(pDX);

	DDX_Control(pDX, IDC_QR_CODE, m_CtrlQRCode);
}

BOOL CQRCodeDlg::OnInitDialog()
{
	CDialogFx::OnInitDialog();

	SetWindowText(i18n(_T("WindowTitle"), _T("QRCODE")));

	m_bShowWindow = TRUE;

	UpdateDialogSize();

	CenterWindow();
	ShowWindow(SW_SHOW);
	return TRUE;
}

BEGIN_MESSAGE_MAP(CQRCodeDlg, CDialogFx)
END_MESSAGE_MAP()

void CQRCodeDlg::UpdateDialogSize()
{
	CDialogFx::UpdateDialogSize();
	m_bHighContrast = FALSE;

	ChangeZoomType(m_ZoomType);

	CImage image;
	int width = 0;
	int height = 0;
	if (image.Load(m_QRCodePath) == S_OK)
	{
		width = image.GetWidth();
		height = image.GetHeight();
	}

	SetClientSize(width + (int)(64 * m_ZoomRatio), height + (int)(64 * m_ZoomRatio), 1.0);
	UpdateBackground(TRUE, m_bDarkMode);

	m_CtrlQRCode.InitControl((int)(32 * m_ZoomRatio), (int)(32 * m_ZoomRatio), width, height, 1.0, m_hPal, &m_BkDC, m_QRCodePath, 1, BS_CENTER, OwnerDrawImage, FALSE, FALSE, FALSE);

	Invalidate();
}