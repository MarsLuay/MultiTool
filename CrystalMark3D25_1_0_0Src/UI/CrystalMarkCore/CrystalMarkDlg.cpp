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
#include "QRCodeDlg.h"

#include <math.h>
#include <afxinet.h>
#pragma comment(lib, "wininet.lib")

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

extern PROCESS_INFORMATION pi;

extern UINT(*ExecBenchmark0)(LPVOID);
extern UINT(*ExecBenchmark1)(LPVOID);
extern UINT(*ExecBenchmark2)(LPVOID);
extern UINT(*ExecBenchmark3)(LPVOID);
extern UINT(*ExecBenchmark4)(LPVOID);

CCrystalMarkDlg::CCrystalMarkDlg(CWnd* pParent /*=NULL*/)
	: CMainDialogFx(CCrystalMarkDlg::IDD, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);

#if _MSC_VER <= 1310
	if (IsNT4() || IsWin95())
	{
		m_hIconMini = (HICON)::LoadImage(AfxGetInstanceHandle(), MAKEINTRESOURCE(IDR_MAINFRAME),
											IMAGE_ICON, 16,  16, LR_VGACOLOR);
	}
	else
#endif
	{
		m_hIconMini = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
	}

	m_SizeX = SIZE_X;
	m_SizeY = SIZE_Y;

#if _MSC_VER > 1310
	m_AdminMode = IsUserAnAdmin();
#else
	#ifdef UNICODE
		m_AdminMode = IsUserAdmin();
	#else
		m_AdminMode = FALSE;
	#endif
#endif

/*
#ifdef UNICODE
	if (IsNT4() || IsWin9x())
	{
		m_MainUIinEnglish = GetPrivateProfileInt(_T("Setting"), _T("MainUIinEnglish"), 1, m_Ini);
	}
	else
	{
		m_MainUIinEnglish = GetPrivateProfileInt(_T("Setting"), _T("MainUIinEnglish"), 0, m_Ini);
	}
#else
	m_MainUIinEnglish = GetPrivateProfileInt(_T("Setting"), _T("MainUIinEnglish"), 1, m_Ini);
#endif
*/
	m_MainUIinEnglish = GetPrivateProfileInt(_T("Setting"), _T("MainUIinEnglish"), 0, m_Ini);

	m_WinThread = NULL;
	m_BenchStatus = FALSE;

	for (int i = 0; i < 5; i++)
	{
		for (int j = 0; j < 5; j++)
		{
			m_Score[i][j] = 0;
		}
	}

	m_CtrlScore[0][0] = &m_CtrlScore0_0;

	m_CtrlScore[1][0] = &m_CtrlScore1_0;
	m_CtrlScore[2][0] = &m_CtrlScore2_0;
	m_CtrlScore[3][0] = &m_CtrlScore3_0;
	m_CtrlScore[4][0] = &m_CtrlScore4_0;

	m_CtrlScore[1][1] = &m_CtrlScore1_1;
	m_CtrlScore[1][2] = &m_CtrlScore1_2;
	m_CtrlScore[1][3] = &m_CtrlScore1_3;
	m_CtrlScore[1][4] = &m_CtrlScore1_4;

	m_CtrlScore[2][1] = &m_CtrlScore2_1;
	m_CtrlScore[2][2] = &m_CtrlScore2_2;
	m_CtrlScore[2][3] = &m_CtrlScore2_3;
	m_CtrlScore[2][4] = &m_CtrlScore2_4;

	m_CtrlScore[3][1] = &m_CtrlScore3_1;
	m_CtrlScore[3][2] = &m_CtrlScore3_2;
	m_CtrlScore[3][3] = &m_CtrlScore3_3;
	m_CtrlScore[3][4] = &m_CtrlScore3_4;

	m_CtrlScore[4][1] = &m_CtrlScore4_1;
	m_CtrlScore[4][2] = &m_CtrlScore4_2;
	m_CtrlScore[4][3] = &m_CtrlScore4_3;
	m_CtrlScore[4][4] = &m_CtrlScore4_4;

	m_AboutDlg = NULL;

#ifdef SUISHO_AOI_SUPPORT
	m_DefaultTheme = _T("Aoi");
	m_RecommendTheme = _T("AoiLightAnimalEars~TenmuShinryuusai");
	m_ThemeKeyName = _T("ThemeAoi");

	m_MarginButtonTop = 16;
	m_MarginButtonLeft = 0;
	m_MarginButtonBottom = 16;
	m_MarginButtonRight = 0;
	m_MarginMeterTop = 4;
	m_MarginMeterLeft = 8;
	m_MarginMeterBottom = 4;
	m_MarginMeterRight = 8;
	m_MarginCommentTop = 12;
	m_MarginCommentLeft = 8;
	m_MarginCommentBottom = 0;
	m_MarginCommentRight = 56;

#elif SUISHO_SHIZUKU_SUPPORT
	m_DefaultTheme = _T("Shizuku");
	m_RecommendTheme = _T("ShizukuLightAnimalEars~TenmuShinryuusai");
	m_ThemeKeyName = _T("ThemeShizuku");

	m_MarginButtonTop = 16;
	m_MarginButtonLeft = 0;
	m_MarginButtonBottom = 16;
	m_MarginButtonRight = 0;
	m_MarginButtonRight = 0;
	m_MarginMeterTop = 4;
	m_MarginMeterLeft = 16;
	m_MarginMeterBottom = 4;
	m_MarginMeterRight = 12;
	m_MarginCommentTop = 0;
	m_MarginCommentLeft = 16;
	m_MarginCommentBottom = 0;
	m_MarginCommentRight = 16;
#else
	m_DefaultTheme = _T("Default");
	m_RecommendTheme = _T("Default");
	m_ThemeKeyName = _T("Theme");

	m_MarginButtonTop = 16;
	m_MarginButtonLeft = 0;
	m_MarginButtonBottom = 16;
	m_MarginButtonRight = 0;
	m_MarginMeterTop = 4;
	m_MarginMeterLeft = 8;
	m_MarginMeterBottom = 4;
	m_MarginMeterRight = 8;
	m_MarginCommentTop = 0;
	m_MarginCommentLeft = 8;
	m_MarginCommentBottom = 0;
	m_MarginCommentRight = 8;
#endif

	m_BackgroundName = _T("Background");
	m_RandomThemeLabel = _T("Random");
	m_RandomThemeName = _T("");

	m_Cores = 1;
	m_Threads = 1;

	// Ranking System
	m_RsCpu = _T("");
	m_RsCpuClock = 0;
	m_RsCpuCore = 0;
	m_RsCpuThread = 0;
	m_RsGpu = _T("");
	m_RsGpuVram = 0;
	m_RsComputerSystem = _T("");;
	m_RsBaseBoard = _T("");
	m_RsOsName = _T("");
	m_RsOsVersion = _T("");
	m_RsOsArchitecture = _T("");
	m_RsScreenWidth = 0;
	m_RsScreenHeight = 0;
	m_RsScreenColor = 0;
	m_RsScreenSmoothing = _T("");
	m_RsMemorySize = 0;
}

CCrystalMarkDlg::~CCrystalMarkDlg()
{
#ifdef SUISHO_SHIZUKU_SUPPORT
	AlertSound(_T(""), 0);
#endif
}

void CCrystalMarkDlg::DoDataExchange(CDataExchange* pDX)
{
	CMainDialogFx::DoDataExchange(pDX);

	DDX_Control(pDX, IDC_START_0, m_CtrlStart0);
	DDX_Control(pDX, IDC_START_1, m_CtrlStart1);
	DDX_Control(pDX, IDC_START_2, m_CtrlStart2);
	DDX_Control(pDX, IDC_START_3, m_CtrlStart3);
	DDX_Control(pDX, IDC_START_4, m_CtrlStart4);

	DDX_Control(pDX, IDC_SCORE_0_0, m_CtrlScore0_0);

	DDX_Control(pDX, IDC_SCORE_1_0, m_CtrlScore1_0);
	DDX_Control(pDX, IDC_SCORE_2_0, m_CtrlScore2_0);
	DDX_Control(pDX, IDC_SCORE_3_0, m_CtrlScore3_0);
	DDX_Control(pDX, IDC_SCORE_4_0, m_CtrlScore4_0);

	DDX_Control(pDX, IDC_SCORE_1_1, m_CtrlScore1_1);
	DDX_Control(pDX, IDC_SCORE_1_2, m_CtrlScore1_2);
	DDX_Control(pDX, IDC_SCORE_1_3, m_CtrlScore1_3);
	DDX_Control(pDX, IDC_SCORE_1_4, m_CtrlScore1_4);

	DDX_Control(pDX, IDC_SCORE_2_1, m_CtrlScore2_1);
	DDX_Control(pDX, IDC_SCORE_2_2, m_CtrlScore2_2);
	DDX_Control(pDX, IDC_SCORE_2_3, m_CtrlScore2_3);
	DDX_Control(pDX, IDC_SCORE_2_4, m_CtrlScore2_4);

	DDX_Control(pDX, IDC_SCORE_3_1, m_CtrlScore3_1);
	DDX_Control(pDX, IDC_SCORE_3_2, m_CtrlScore3_2);
	DDX_Control(pDX, IDC_SCORE_3_3, m_CtrlScore3_3);
	DDX_Control(pDX, IDC_SCORE_3_4, m_CtrlScore3_4);

	DDX_Control(pDX, IDC_SCORE_4_1, m_CtrlScore4_1);
	DDX_Control(pDX, IDC_SCORE_4_2, m_CtrlScore4_2);
	DDX_Control(pDX, IDC_SCORE_4_3, m_CtrlScore4_3);
	DDX_Control(pDX, IDC_SCORE_4_4, m_CtrlScore4_4);

	DDX_Control(pDX, IDC_LABEL_SYSTEM_INFO_1, m_LabelSystemInfo1);
	DDX_Control(pDX, IDC_LABEL_SYSTEM_INFO_2, m_LabelSystemInfo2);
	DDX_Control(pDX, IDC_LABEL_SYSTEM_INFO_3, m_LabelSystemInfo3);
	DDX_Control(pDX, IDC_LABEL_SYSTEM_INFO_4, m_LabelSystemInfo4);
	DDX_Control(pDX, IDC_LABEL_SYSTEM_INFO_5, m_LabelSystemInfo5);
	DDX_Control(pDX, IDC_LABEL_SYSTEM_INFO_6, m_LabelSystemInfo6);

	DDX_Control(pDX, IDC_SYSTEM_INFO_1, m_CtrlSystemInfo1);
	DDX_Control(pDX, IDC_SYSTEM_INFO_2, m_CtrlSystemInfo2);
	DDX_Control(pDX, IDC_SYSTEM_INFO_3, m_CtrlSystemInfo3);
	DDX_Control(pDX, IDC_SYSTEM_INFO_4, m_CtrlSystemInfo4);
	DDX_Control(pDX, IDC_SYSTEM_INFO_5, m_CtrlSystemInfo5);
	DDX_Control(pDX, IDC_SYSTEM_INFO_6, m_CtrlSystemInfo6);

	DDX_Control(pDX, IDC_COMBO_GPU_INFO, m_CtrlGpuInfo);


#if _MSC_VER <= 1310
	DDX_Control(pDX, IDC_COMMENT_UPPER, m_CtrlCommentUpper);
#endif

	DDX_Control(pDX, IDC_COMMENT, m_CtrlComment);

	DDX_Control(pDX, IDC_SNS_1, m_CtrlSns1);
	DDX_Control(pDX, IDC_CRYSTALMARK, m_CtrlCrystalMark);
	DDX_Control(pDX, IDC_SUBMIT, m_CtrlSubmit);
	DDX_Control(pDX, IDC_QR, m_CtrlQR);

#ifdef SUISHO_SHIZUKU_SUPPORT
	DDX_Control(pDX, IDC_SD, m_CtrlSD);
#endif
#if _MSC_VER > 1310
	DDX_Control(pDX, IDC_ADS_1, m_CtrlAds);
#endif
}

BEGIN_MESSAGE_MAP(CCrystalMarkDlg, CMainDialogFx)
//	ON_WM_SIZE()

	ON_COMMAND(ID_COPY, &CCrystalMarkDlg::OnCopy)

#if _MSC_VER > 1310
	ON_COMMAND(ID_SAVE_TEXT, &CCrystalMarkDlg::OnSaveText)
	ON_COMMAND(ID_SAVE_IMAGE, &CCrystalMarkDlg::OnSaveImage)
#endif

	ON_COMMAND(ID_EXIT, &CCrystalMarkDlg::OnExit)
	ON_COMMAND(ID_ABOUT, &CCrystalMarkDlg::OnAbout)

	ON_MESSAGE(WM_UPDATE_SCORE, OnUpdateScore)
	ON_MESSAGE(WM_UPDATE_MESSAGE, OnUpdateMessage)
	ON_MESSAGE(WM_EXIT_BENCHMARK, OnExitBenchmark)
	ON_MESSAGE(WM_START_BENCHMARK, OnStartBenchmark)
	ON_MESSAGE(WM_SECRET_VOICE, OnSecretVoice)

	ON_COMMAND(ID_ZOOM_50, &CCrystalMarkDlg::OnZoom50)
	ON_COMMAND(ID_ZOOM_64, &CCrystalMarkDlg::OnZoom64)
	ON_COMMAND(ID_ZOOM_75, &CCrystalMarkDlg::OnZoom75)
	ON_COMMAND(ID_ZOOM_100, &CCrystalMarkDlg::OnZoom100)
	ON_COMMAND(ID_ZOOM_125, &CCrystalMarkDlg::OnZoom125)
	ON_COMMAND(ID_ZOOM_150, &CCrystalMarkDlg::OnZoom150)
	ON_COMMAND(ID_ZOOM_200, &CCrystalMarkDlg::OnZoom200)
	ON_COMMAND(ID_ZOOM_250, &CCrystalMarkDlg::OnZoom250)
	ON_COMMAND(ID_ZOOM_300, &CCrystalMarkDlg::OnZoom300)
	ON_COMMAND(ID_ZOOM_AUTO, &CCrystalMarkDlg::OnZoomAuto)
	ON_COMMAND(ID_HELP, &CCrystalMarkDlg::OnHelp)
	ON_COMMAND(ID_CRYSTALDEWWORLD, &CCrystalMarkDlg::OnCrystalDewWorld)
	ON_COMMAND(ID_CRYSTALMARKDB, &CCrystalMarkDlg::OnCrystalMarkDB)
	ON_COMMAND(ID_FONT_SETTING, &CCrystalMarkDlg::OnFontSetting)

	ON_BN_CLICKED(IDC_SD, &CCrystalMarkDlg::OnSD)
	ON_BN_CLICKED(IDC_SUBMIT, &CCrystalMarkDlg::OnSubmit)
	ON_BN_CLICKED(IDC_QR, &CCrystalMarkDlg::OnQR)
	ON_BN_CLICKED(IDC_SNS_1, &CCrystalMarkDlg::OnTweet)
	ON_BN_CLICKED(IDC_ADS_1, &CCrystalMarkDlg::OnAds)
	ON_BN_CLICKED(IDC_START_0, &CCrystalMarkDlg::OnStart0)
	ON_BN_CLICKED(IDC_START_1, &CCrystalMarkDlg::OnStart1)
	ON_BN_CLICKED(IDC_START_2, &CCrystalMarkDlg::OnStart2)
	ON_BN_CLICKED(IDC_START_3, &CCrystalMarkDlg::OnStart3)
	ON_BN_CLICKED(IDC_START_4, &CCrystalMarkDlg::OnStart4)


	ON_COMMAND(ID_MAIN_UI_IN_ENGLISH, &CCrystalMarkDlg::OnMainUIinEnglish)

#ifdef SUISHO_AOI_SUPPORT
	ON_COMMAND(ID_VOICE_ENGLISH, &CCrystalMarkDlg::OnVoiceEnglish)
	ON_COMMAND(ID_VOICE_JAPANESE, &CCrystalMarkDlg::OnVoiceJapanese)
#endif

#ifdef SUISHO_SHIZUKU_SUPPORT
	ON_COMMAND(ID_VOICE_VOLUME_000, &CCrystalMarkDlg::OnVoiceVolume000)
	ON_COMMAND(ID_VOICE_VOLUME_010, &CCrystalMarkDlg::OnVoiceVolume010)
	ON_COMMAND(ID_VOICE_VOLUME_020, &CCrystalMarkDlg::OnVoiceVolume020)
	ON_COMMAND(ID_VOICE_VOLUME_030, &CCrystalMarkDlg::OnVoiceVolume030)
	ON_COMMAND(ID_VOICE_VOLUME_040, &CCrystalMarkDlg::OnVoiceVolume040)
	ON_COMMAND(ID_VOICE_VOLUME_050, &CCrystalMarkDlg::OnVoiceVolume050)
	ON_COMMAND(ID_VOICE_VOLUME_060, &CCrystalMarkDlg::OnVoiceVolume060)
	ON_COMMAND(ID_VOICE_VOLUME_070, &CCrystalMarkDlg::OnVoiceVolume070)
	ON_COMMAND(ID_VOICE_VOLUME_080, &CCrystalMarkDlg::OnVoiceVolume080)
	ON_COMMAND(ID_VOICE_VOLUME_090, &CCrystalMarkDlg::OnVoiceVolume090)
	ON_COMMAND(ID_VOICE_VOLUME_100, &CCrystalMarkDlg::OnVoiceVolume100)
#endif

END_MESSAGE_MAP()

LRESULT CCrystalMarkDlg::OnQueryEndSession(WPARAM wParam, LPARAM lParam)
{
	return TRUE;
}

BOOL CCrystalMarkDlg::CheckThemeEdition(CString name)
{
#ifdef SUISHO_AOI_SUPPORT
	if(name.Find(_T("Aoi")) == 0) { return TRUE; }
#elif SUISHO_SHIZUKU_SUPPORT
	if(name.Find(_T("Shizuku")) == 0) { return TRUE; }
#else
	if(name.Find(_T("Shizuku")) != 0 && name.Find(_T("Aoi")) != 0 && name.Find(_T(".")) != 0) { return TRUE; }
#endif

	return FALSE;
}

CString CCrystalMarkDlg::VOICE(__int64 score)
{
	CString voiceName;
	if (score >= 100000)
	{
		voiceName = _T("Mark01");
	}
	else if (score >= 50000)
	{
		voiceName = _T("Mark02");
	}
	else if (score >= 10000)
	{
		voiceName = _T("Mark03");
	}
	else if (score >= 5000)
	{
		voiceName = _T("Mark04");
	}
	else if (score >= 1000)
	{
		voiceName = _T("Mark05");
	}
	else if (score > 0)
	{
		voiceName = _T("Mark06");
	}
	else if (score == 0)
	{
		switch (rand() % 5)
		{
		case 0: voiceName = _T("Mark07"); break;
		case 1: voiceName = _T("Mark08"); break;
		case 2: voiceName = _T("Mark09"); break;
		case 3: voiceName = _T("Mark10"); break;
		default:voiceName = _T("Mark11"); break;
		}
	}
	else if (score == -1) // Secret Voice
	{
		voiceName = _T("Mark12");
	}

	CString voicePath;
	voicePath.Format(_T("%s%s\\%s.wav"), (LPCTSTR)m_VoiceDir, (LPCTSTR)m_CurrentVoice, (LPCTSTR)voiceName);
	if (IsFileExist(voicePath))
	{
		return voicePath;
	}

	voicePath.Format(_T("%s%s\\%s.mp3"), (LPCTSTR)m_VoiceDir, (LPCTSTR)m_CurrentVoice, (LPCTSTR)voiceName);
	if (IsFileExist(voicePath))
	{
		return voicePath;
	}

	return _T("");
}


CString CCrystalMarkDlg::SD(__int64 score)
{
#ifdef SUISHO_SHIZUKU_SUPPORT
	CString imageName;

#ifdef SUISHO_AOI_SUPPORT
	#define SD_CHARACTER _T("SDAoi")
#elif SUISHO_SHIZUKU_SUPPORT
	#define SD_CHARACTER _T("SDShizuku")
#endif

	if (score >= 50000)
	{
		imageName.Format(_T("%s%s"), SD_CHARACTER, _T("S"));
	}
	else if (score >= 10000)
	{
		imageName.Format(_T("%s%s"), SD_CHARACTER, _T("A"));
	}
	else if (score >= 5000)
	{
		imageName.Format(_T("%s%s"), SD_CHARACTER, _T("B"));
	}
	else if (score >= 1000)
	{
		imageName.Format(_T("%s%s"), SD_CHARACTER, _T("C"));
	}
	else if (score > 0)
	{
		imageName.Format(_T("%s%s"), SD_CHARACTER, _T("D"));
	}
	else if (score == 0)
	{
		imageName.Format(_T("%s%s"), SD_CHARACTER, _T("E"));
	}

	CString imagePath;
	imagePath.Format(_T("%s%s\\%s-%03d.png"), (LPCTSTR)m_ThemeDir, (LPCTSTR)m_CurrentTheme, (LPCTSTR)imageName, (DWORD)(m_ZoomRatio * 100));
	if (IsFileExist(imagePath))
	{
		return imagePath;
	}
	imagePath.Format(_T("%s%s\\%s-%03d.png"), (LPCTSTR)m_ThemeDir, (LPCTSTR)m_ParentTheme1, (LPCTSTR)imageName, (DWORD)(m_ZoomRatio * 100));
	if (IsFileExist(imagePath))
	{
		return imagePath;
	}
	imagePath.Format(_T("%s%s\\%s-%03d.png"), (LPCTSTR)m_ThemeDir, (LPCTSTR)m_ParentTheme2, (LPCTSTR)imageName, (DWORD)(m_ZoomRatio * 100));
	if (IsFileExist(imagePath))
	{
		return imagePath;
	}
	imagePath.Format(_T("%s%s\\%s-%03d.png"), (LPCTSTR)m_ThemeDir, (LPCTSTR)m_DefaultTheme, (LPCTSTR)imageName, (DWORD)(m_ZoomRatio * 100));
	if (IsFileExist(imagePath))
	{
		return imagePath;
	}
#endif
	return _T("");
}


void CCrystalMarkDlg::UpdateThemeInfo()
{
	CMainDialogFx::UpdateThemeInfo();

	CString theme = m_ThemeDir + m_CurrentTheme + _T("\\theme.ini");

#ifdef SUISHO_AOI_SUPPORT
	m_MarginButtonTop = GetPrivateProfileInt(_T("Margin"), _T("ButtonTop"), 16, theme);
	m_MarginButtonLeft = GetPrivateProfileInt(_T("Margin"), _T("ButtonLeft"), 0, theme);
	m_MarginButtonBottom = GetPrivateProfileInt(_T("Margin"), _T("ButtonBottom"), 16, theme);
	m_MarginButtonRight = GetPrivateProfileInt(_T("Margin"), _T("ButtonRight"), 0, theme);
	m_MarginMeterTop = GetPrivateProfileInt(_T("Margin"), _T("MeterTop"), 0, theme);
	m_MarginMeterLeft = GetPrivateProfileInt(_T("Margin"), _T("MeterLeft"), 0, theme);
	m_MarginMeterBottom = GetPrivateProfileInt(_T("Margin"), _T("MeterBottom"), 0, theme);
	m_MarginMeterRight = GetPrivateProfileInt(_T("Margin"), _T("MeterRight"), 16, theme);
	m_MarginCommentTop = GetPrivateProfileInt(_T("Margin"), _T("CommentTop"), 12, theme);
	m_MarginCommentLeft = GetPrivateProfileInt(_T("Margin"), _T("CommentLeft"), 16, theme);
	m_MarginCommentBottom = GetPrivateProfileInt(_T("Margin"), _T("CommentBottom"), 0, theme);
	m_MarginCommentRight = GetPrivateProfileInt(_T("Margin"), _T("CommentRight"), 64, theme);

#elif SUISHO_SHIZUKU_SUPPORT
	m_MarginButtonTop = GetPrivateProfileInt(_T("Margin"), _T("ButtonTop"), 8, theme);
	m_MarginButtonLeft = GetPrivateProfileInt(_T("Margin"), _T("ButtonLeft"), 0, theme);
	m_MarginButtonBottom = GetPrivateProfileInt(_T("Margin"), _T("ButtonBottom"), 8, theme);
	m_MarginButtonRight = GetPrivateProfileInt(_T("Margin"), _T("ButtonRight"), 0, theme);
	m_MarginMeterTop = GetPrivateProfileInt(_T("Margin"), _T("MeterTop"), 0, theme);
	m_MarginMeterLeft = GetPrivateProfileInt(_T("Margin"), _T("MeterLeft"), 0, theme);
	m_MarginMeterBottom = GetPrivateProfileInt(_T("Margin"), _T("MeterBottom"), 0, theme);
	m_MarginMeterRight = GetPrivateProfileInt(_T("Margin"), _T("MeterRight"), 16, theme);
	m_MarginCommentTop = GetPrivateProfileInt(_T("Margin"), _T("CommentTop"), 0, theme);
	m_MarginCommentLeft = GetPrivateProfileInt(_T("Margin"), _T("CommentLeft"), 16, theme);
	m_MarginCommentBottom = GetPrivateProfileInt(_T("Margin"), _T("CommentBottom"), 0, theme);
	m_MarginCommentRight = GetPrivateProfileInt(_T("Margin"), _T("CommentRight"), 16, theme);

#else
	m_MarginButtonTop = GetPrivateProfileInt(_T("Margin"), _T("ButtonTop"), 4, theme);
	m_MarginButtonLeft = GetPrivateProfileInt(_T("Margin"), _T("ButtonLeft"), 0, theme);
	m_MarginButtonBottom = GetPrivateProfileInt(_T("Margin"), _T("ButtonBottom"), 4, theme);
	m_MarginButtonRight = GetPrivateProfileInt(_T("Margin"), _T("ButtonRight"), 0, theme);
	m_MarginMeterTop = GetPrivateProfileInt(_T("Margin"), _T("MeterTop"), 0, theme);
	m_MarginMeterLeft = GetPrivateProfileInt(_T("Margin"), _T("MeterLeft"), 0, theme);
	m_MarginMeterBottom = GetPrivateProfileInt(_T("Margin"), _T("MeterBottom"), 0, theme);
	m_MarginMeterRight = GetPrivateProfileInt(_T("Margin"), _T("MeterRight"), 4, theme);
	m_MarginCommentTop = GetPrivateProfileInt(_T("Margin"), _T("CommentTop"), 0, theme);
	m_MarginCommentLeft = GetPrivateProfileInt(_T("Margin"), _T("CommentLeft"), 8, theme);
	m_MarginCommentBottom = GetPrivateProfileInt(_T("Margin"), _T("CommentBottom"), 0, theme);
	m_MarginCommentRight = GetPrivateProfileInt(_T("Margin"), _T("CommentRight"), 8, theme);

#endif
}

BOOL CCrystalMarkDlg::OnInitDialog()
{
	CMainDialogFx::OnInitDialog();

	m_hAccelerator = ::LoadAccelerators(AfxGetInstanceHandle(),
		MAKEINTRESOURCE(IDR_ACCELERATOR));

	SetIcon(m_hIcon, TRUE);
	SetIcon(m_hIconMini, FALSE);

	TCHAR str[256];
	GetPrivateProfileString(_T("Setting"), _T("FontFace"), GetDefaultFont(), str, 256, m_Ini);
	m_FontFace = str;

	m_FontScale = GetPrivateProfileInt(_T("Setting"), _T("FontScale"), 100, m_Ini);
	if (m_FontScale > 150 || m_FontScale < 50)
	{
		m_FontScale = 100;
		m_FontRatio = 1.0;
	}
	else
	{
		m_FontRatio = m_FontScale / 100.0;
	}

	m_FontRender = (BYTE)GetPrivateProfileInt(L"Setting", L"FontRender", CLEARTYPE_NATURAL_QUALITY, m_Ini);
	if (m_FontRender > CLEARTYPE_NATURAL_QUALITY)
	{
		m_FontRender = CLEARTYPE_NATURAL_QUALITY;
	}

	InitThemeLang();
	InitMenu();
	ChangeTheme(m_CurrentTheme);
	ChangeLang(m_CurrentLang);
	UpdateThemeInfo();

	UpdateData(FALSE);

	ChangeZoomType(m_ZoomType);
	switch (GetPrivateProfileInt(_T("Setting"), _T("ZoomType"), 0, m_Ini))
	{
	case  50:  CheckRadioZoomType(ID_ZOOM_50,   50); break;
	case  64:  CheckRadioZoomType(ID_ZOOM_64,   64); break;
	case  75:  CheckRadioZoomType(ID_ZOOM_75,   75); break;
	case 100:  CheckRadioZoomType(ID_ZOOM_100, 100); break;
	case 125:  CheckRadioZoomType(ID_ZOOM_125, 125); break;
	case 150:  CheckRadioZoomType(ID_ZOOM_150, 150); break;
	case 200:  CheckRadioZoomType(ID_ZOOM_200, 200); break;
	case 250:  CheckRadioZoomType(ID_ZOOM_250, 250); break;
	case 300:  CheckRadioZoomType(ID_ZOOM_300, 300); break;
	default:   CheckRadioZoomType(ID_ZOOM_AUTO,  0); break;
	}

	// System Information
	CString cpuInfo = _T("");
	CString gpuInfo = _T("");
	CString gpuInfoTooltip = _T("");
	CString baseBoardInfo = _T("");
	CString computerSystemInfo = _T("");
	CString systemInfoTooltip = _T("");
	CString osInfo = _T("");
	CString screenInfo = _T("");
	CString memoryInfo = _T("");

	// CPU
	GetCpuInfo(cpuInfo, m_RsCpu, &m_RsCpuClock, &m_Cores, &m_Threads);
	m_CtrlSystemInfo1.SetWindowText(cpuInfo);
	m_CtrlSystemInfo1.SetToolTipText(cpuInfo);
	m_CpuInfo = cpuInfo;
	m_RsCpuCore = m_Cores;
	m_RsCpuThread = m_Threads;

	// GPU
	GetGpuInfo(gpuInfo);
	m_CtrlSystemInfo2.SetWindowText(gpuInfo);
	gpuInfoTooltip = gpuInfo;
	gpuInfoTooltip.Replace(_T(" | "), _T("\n"));
	m_CtrlSystemInfo2.SetToolTipText(gpuInfoTooltip);
	m_GpuInfo = gpuInfo;

	CStringArray m_GpuList;
	SplitCString(gpuInfo, _T(" | "), m_GpuList);

	for (int i = 0; i < m_GpuList.GetCount(); i++)
	{
		m_CtrlGpuInfo.AddString(m_GpuList.GetAt(i));
	}
	m_CtrlGpuInfo.SetCurSel(0);

	if(m_GpuList.GetCount() <= 1)
	{
		m_CtrlGpuInfo.EnableWindow(FALSE);
		m_CtrlGpuInfo.ShowWindow(SW_HIDE);
	}
	else
	{
		m_CtrlSystemInfo2.ShowWindow(SW_HIDE);
	}
	// Mother Board / Product Name
	GetComputerSystemInfo(computerSystemInfo);
	GetBaseBoardInfo(baseBoardInfo);
	m_RsComputerSystem = computerSystemInfo;
	m_RsBaseBoard = baseBoardInfo;

	if (!computerSystemInfo.IsEmpty() && !baseBoardInfo.IsEmpty() && computerSystemInfo.Compare((LPCTSTR)baseBoardInfo) != 0)
	{
		m_SystemInfo = computerSystemInfo + _T(" | ") + baseBoardInfo;
		systemInfoTooltip = computerSystemInfo + _T("\n") + baseBoardInfo;
		
		// for ASUS
		if (computerSystemInfo.Find(_T("ASUS")) == 0 && computerSystemInfo.GetLength() == 4)
		{
			computerSystemInfo = _T("");
		}
		// Trim //
		computerSystemInfo.Replace(_T("Micro-Star International Co., Ltd."), _T("MSI"));
		computerSystemInfo.Replace(_T("ASUSTeK Computer Inc."), _T("ASUS"));
		computerSystemInfo.Replace(_T("ASUSTeK COMPUTER INC."), _T("ASUS"));
		computerSystemInfo.Replace(_T("Gigabyte Technology Co., Ltd"), _T("Gigabyte"));
		computerSystemInfo.Replace(_T("HP HP"), _T("HP"));

		baseBoardInfo.Replace(_T("Micro-Star International Co., Ltd."), _T("MSI"));
		baseBoardInfo.Replace(_T("ASUSTeK Computer Inc."), _T("ASUS"));
		baseBoardInfo.Replace(_T("ASUSTeK COMPUTER INC."), _T("ASUS"));
		baseBoardInfo.Replace(_T("Gigabyte Technology Co., Ltd"), _T("Gigabyte"));
		baseBoardInfo.Replace(_T("HP HP"), _T("HP"));

		if (computerSystemInfo.IsEmpty())
		{
			m_CtrlSystemInfo3.SetWindowText(baseBoardInfo);
		}
		else
		{
			m_CtrlSystemInfo3.SetWindowText(computerSystemInfo + _T(" | ") + baseBoardInfo);
		}
	}
	else if (! baseBoardInfo.IsEmpty())
	{
		m_CtrlSystemInfo3.SetWindowText(baseBoardInfo);
		m_SystemInfo = baseBoardInfo;
		systemInfoTooltip = baseBoardInfo;
	}
	else
	{
		m_CtrlSystemInfo3.SetWindowText(computerSystemInfo);
		m_SystemInfo = computerSystemInfo;
		systemInfoTooltip = computerSystemInfo;
	}
	m_CtrlSystemInfo3.SetToolTipText(systemInfoTooltip);

	GetOsName(osInfo, m_RsOsName, m_RsOsVersion, m_RsOsArchitecture);
	if (IsRunningOnWine() || m_SystemInfo.Find(_T("The Wine")) == 0)
	{
		osInfo = _T("[Wine] ") + osInfo;
		m_RsOsName = _T("[Wine] ") + m_RsOsName;
	}

	CString hypervisor = _T("");
//	if (m_GpuInfo.Find(_T("VMware")) == 0) { hypervisor = _T("[VMware] "); } QEMU/VMware
	if (m_GpuInfo.Find(_T("VBox")) == 0) { hypervisor = _T("[VirtualBox] "); }
	if (m_GpuInfo.Find(_T("Parallels")) == 0) { hypervisor = _T("[Parallels] "); }
	if (m_GpuInfo.Find(_T("VirtIO")) == 0) { hypervisor = _T("[QEMU] "); }
	if (m_SystemInfo.Find(_T("VMware")) == 0) { hypervisor = _T("[VMware] "); }
	if (m_SystemInfo.Find(_T("QEMU")) == 0) { hypervisor = _T("[QEMU] "); }

#if defined(_M_IX86) || defined(_M_X64)
	char vendorString[13] = { 0 };
	GetHypervisorVendorString(vendorString);

	if (strcmp(vendorString, "Microsoft Hv") == 0)
	{
		unsigned int eax = 0;
		unsigned int ebx = 0;
		unsigned int ecx = 0;
		unsigned int edx = 0;
		
		GetCpuid(0x40000000, &eax, &ebx, &ecx, &edx);
		if (eax >= 0x40000003)
		{
			GetCpuid(0x40000003, &eax, &ebx, &ecx, &edx);
			if (! (ebx & 0x00001000))
			{
				hypervisor = _T("[Hyper-V] ");
			}
		}
	}
	else if (strcmp(vendorString, "VMwareVMware") == 0) { hypervisor = _T("[VMware] "); }
	else if (strcmp(vendorString, "KVMKVMKVM") == 0)    { hypervisor = _T("[KVM] "); }
	else if (strcmp(vendorString, "XenVMMXenVMM") == 0) { hypervisor = _T("[Xen] "); }
	else if (strcmp(vendorString, "QEMUQEMUQEMU") == 0) { hypervisor = _T("[QEMU] "); }
	else if (strcmp(vendorString, "TCGTCGTCGTCG") == 0) { hypervisor = _T("[QEMU] "); }
	else if (strcmp(vendorString, "VBoxVBoxVBox") == 0) { hypervisor = _T("[VirtualBox] "); }
	else if (strcmp(vendorString, "prl prl prl ") == 0) { hypervisor = _T("[Parallels] "); }
	else if (strcmp(vendorString, "prl hyperv ") == 0)  { hypervisor = _T("[Parallels] "); }
	else if (strcmp(vendorString, "lrpepyh vr") == 0)   { hypervisor = _T("[Parallels] "); }
	else if (strcmp(vendorString, "bhyve bhyve ") == 0) { hypervisor = _T("[Bhyve] "); }
	else if (strcmp(vendorString, "ACRNACRNACRN") == 0) { hypervisor = _T("[ACRN] "); }
	else if (strcmp(vendorString, "QNXQVMBSQG") == 0)   { hypervisor = _T("[QNX] "); }
#endif
	osInfo = hypervisor + osInfo;
	m_RsOsName = hypervisor + m_RsOsName;

	m_CtrlSystemInfo4.SetWindowText(osInfo);
	m_OsInfo = osInfo;

	GetScreenInfo(screenInfo, &m_RsScreenWidth, &m_RsScreenHeight, &m_RsScreenColor, m_RsScreenSmoothing);
	m_CtrlSystemInfo5.SetWindowText(screenInfo);
	m_ScreenInfo = screenInfo;

	GetMemoryInfo(memoryInfo, &m_RsMemorySize);
	m_CtrlSystemInfo6.SetWindowText(memoryInfo);
	m_MemoryInfo = memoryInfo;	
	
	SetWindowTitle(_T(""));

	m_bShowWindow = TRUE;
	RestoreWindowPosition();
	ChangeZoomType(m_ZoomType);
	UpdateDialogSize();

	m_bInitializing = FALSE;

	SetForegroundWindow();

	/*
	// Error Check
	TCHAR path[MAX_PATH];
	::GetModuleFileName(NULL, path, MAX_PATH);
	if (!CheckCodeSign(CERTNAME, path))
	{
		AfxMessageBox(m_MesExeFileModified);
		OnExit();
		return FALSE;
	}
	*/

	return TRUE;
}

void CCrystalMarkDlg::SetClientSize(int sizeX, int sizeY, double zoomRatio)
{
	RECT rw, rc;
	GetWindowRect(&rw);
	GetClientRect(&rc);

	if (rc.right != 0)
	{
		int ncaWidth = (rw.right - rw.left) - (rc.right - rc.left);
		int ncaHeight = (rw.bottom - rw.top) - (rc.bottom - rc.top);

		m_MinSizeX = (int)(sizeX * zoomRatio) + ncaWidth;
		m_MaxSizeX = m_MinSizeX;
		m_MinSizeY = (int)(SIZE_MIN_Y * m_ZoomRatio + ncaHeight);
		m_MaxSizeY = (int)(SIZE_MAX_Y * m_ZoomRatio + ncaHeight);

		SetWindowPos(NULL, 0, 0, (int)(sizeX * zoomRatio) + ncaWidth, (int)(sizeY * zoomRatio) + ncaHeight, SWP_NOMOVE | SWP_NOZORDER);
	}
}

BOOL CCrystalMarkDlg::PreTranslateMessage(MSG* pMsg) 
{
	if( 0 != ::TranslateAccelerator(m_hWnd, m_hAccelerator, pMsg) )
	{
		return TRUE;
	}

	return CDialog::PreTranslateMessage(pMsg);
}

void CCrystalMarkDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CMainDialogFx::OnPaint();
	}
}

HCURSOR CCrystalMarkDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}

void CCrystalMarkDlg::OnOK()
{
}

void CCrystalMarkDlg::OnCancel()
{
	if (m_BenchStatus)
	{
		Stop();
		return;
	}

	SaveWindowPosition();
	CMainDialogFx::OnCancel();
	TerminateProcess(GetCurrentProcess(), 0);
}

void CCrystalMarkDlg::OnExit()
{
	OnCancel();
}

void CCrystalMarkDlg::OnCopy()
{
	SaveText(_T(""));
}
#if _MSC_VER > 1310
void CCrystalMarkDlg::OnSaveText()
{

	CString path;
	SYSTEMTIME st;
	GetLocalTime(&st);
	path.Format(_T("%s_%04d%02d%02d%0d%02d%02d"), PRODUCT_FILENAME, st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond);

	CString filter = _T("TEXT (*.txt)|*.txt||");
	CFileDialog save(FALSE, _T("txt"), path, OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT | OFN_EXPLORER, filter);

	if (save.DoModal() == IDOK)
	{
		SaveText(save.GetPathName());
	}
}

void CCrystalMarkDlg::OnSaveImage()
{
	SaveImage();
}
#endif

void CCrystalMarkDlg::OnAbout()
{
	m_AboutDlg = new CAboutDlg(this);
	m_AboutDlg->Create(CAboutDlg::IDD, m_AboutDlg, ID_ABOUT, this);
}

void CCrystalMarkDlg::EnableMenus()
{
	CMenu *menu = GetMenu();
	for (int i = 0; i < (int)menu->GetMenuItemCount(); i++)
	{
		menu->EnableMenuItem(i, MF_BYPOSITION | MF_ENABLED);
	}
	SetMenu(menu);
}

void CCrystalMarkDlg::DisableMenus()
{
	CMenu* menu = GetMenu();
	for (int i = 0; i < (int)menu->GetMenuItemCount(); i++)
	{
		menu->EnableMenuItem(i, MF_BYPOSITION | MF_GRAYED);
	}
	SetMenu(menu);
}

void CCrystalMarkDlg::ChangeLang(CString LangName)
{
#ifdef UNICODE
	m_CurrentLangPath.Format(_T("%s\\%s.lang"), (LPCTSTR)m_LangDir, (LPCTSTR)LangName);
#else
	m_CurrentLangPath.Format(_T("%s\\%s9x.lang"), (LPCTSTR)m_LangDir, (LPCTSTR)LangName);
#endif

	CString cstr;
	CMenu *menu = GetMenu();
	CMenu subMenu;

	cstr = i18n(_T("Menu"), _T("FILE"));
	MENU_MODIFY_MENU(0, MF_BYPOSITION | MF_STRING, 0, cstr);
	cstr = i18n(_T("Menu"), _T("THEME"));
	MENU_MODIFY_MENU(1, MF_BYPOSITION | MF_STRING, 1, cstr);
	cstr = i18n(_T("Menu"), _T("HELP"));
	MENU_MODIFY_MENU(2, MF_BYPOSITION | MF_STRING, 2, cstr);
	cstr = i18n(_T("Menu"), _T("LANGUAGE"));
	if(cstr.Find(_T("Language")) >= 0)
	{
		cstr = _T("&Language");
		MENU_MODIFY_MENU(3, MF_BYPOSITION | MF_STRING, 3, cstr);
	}
	else
	{
		MENU_MODIFY_MENU(3, MF_BYPOSITION | MF_STRING, 3, cstr + _T(" (&Language)"));
	}

	// File
	cstr = i18n(_T("Menu"), _T("COPY"));
	cstr += _T("\tCtrl + Shift + C");
	MENU_MODIFY_MENU(ID_COPY, MF_STRING, ID_COPY, cstr);
	cstr = i18n(_T("Menu"), _T("SAVE_TEXT"));
	cstr += _T("\tCtrl + T");
	MENU_MODIFY_MENU(ID_SAVE_TEXT, MF_STRING, ID_SAVE_TEXT, cstr);
	cstr = i18n(_T("Menu"), _T("SAVE_IMAGE"));
	cstr += _T("\tCtrl + S");
	MENU_MODIFY_MENU(ID_SAVE_IMAGE, MF_STRING, ID_SAVE_IMAGE, cstr);
	cstr = i18n(_T("Menu"), _T("EXIT"));
	cstr += _T("\tAlt + F4");
	MENU_MODIFY_MENU(ID_EXIT, MF_STRING, ID_EXIT, cstr);


	cstr = i18n(_T("Menu"), _T("HELP")) + _T("\tF1");
	MENU_MODIFY_MENU(ID_HELP, MF_STRING, ID_HELP, cstr);
	cstr = i18n(_T("Menu"), _T("ABOUT"));
	MENU_MODIFY_MENU(ID_ABOUT, MF_STRING, ID_ABOUT, cstr);

	// Theme
	subMenu.Attach(menu->GetSubMenu(MENU_THEME_INDEX)->GetSafeHmenu());
	cstr = i18n(_T("Menu"), _T("ZOOM"));
	SUBMENU_MODIFY_MENU(0, MF_BYPOSITION, 0, cstr);
	subMenu.Detach();

	cstr = i18n(_T("Menu"), _T("AUTO"));
	MENU_MODIFY_MENU(ID_ZOOM_AUTO, MF_STRING, ID_ZOOM_AUTO, cstr);

	cstr = i18n(_T("Menu"), _T("FONT_SETTING")) + _T("\tCtrl + F");
	MENU_MODIFY_MENU(ID_FONT_SETTING, MF_STRING, ID_FONT_SETTING, cstr);

	CheckRadioZoomType();

	cstr = i18n(_T("Menu"), _T("MAIN_UI_IN_ENGLISH"));
	MENU_MODIFY_MENU(ID_MAIN_UI_IN_ENGLISH, MF_STRING, ID_MAIN_UI_IN_ENGLISH, cstr);


	if (m_MainUIinEnglish)
	{
		menu->CheckMenuItem(ID_MAIN_UI_IN_ENGLISH, MF_CHECKED);
	}
	else
	{
		menu->CheckMenuItem(ID_MAIN_UI_IN_ENGLISH, MF_UNCHECKED);
	}

	SetMenu(menu);

	// Message //
	m_MesStopBenchmark = i18n(_T("Message"), _T("STOP_BENCHMARK"));
	m_MesExeFileNotFound = i18n(_T("Message"), _T("EXE_FILE_NOT_FOUND"));
	m_MesExeFileModified = i18n(_T("Message"), _T("EXE_FILE_MODIFIED"));
	m_MesAttachScreenshotManually = i18n(_T("Message"), _T("ATTACH_SCREENSHOT_MANUALLY"));
	m_MesCopyClipboard = i18n(_T("Message"), _T("COPY_CLIPBOARD"));

	// MainWindow
	m_LabelSystemInfo1.SetWindowText(i18n(_T("MainWindow"), _T("SYSTEM_INFO_1"), m_MainUIinEnglish));
	m_LabelSystemInfo2.SetWindowText(i18n(_T("MainWindow"), _T("SYSTEM_INFO_2"), m_MainUIinEnglish));
	m_LabelSystemInfo3.SetWindowText(i18n(_T("MainWindow"), _T("SYSTEM_INFO_3"), m_MainUIinEnglish));
	m_LabelSystemInfo4.SetWindowText(i18n(_T("MainWindow"), _T("SYSTEM_INFO_4"), m_MainUIinEnglish));
	m_LabelSystemInfo5.SetWindowText(i18n(_T("MainWindow"), _T("SYSTEM_INFO_5"), m_MainUIinEnglish));
	m_LabelSystemInfo6.SetWindowText(i18n(_T("MainWindow"), _T("SYSTEM_INFO_6"), m_MainUIinEnglish));

	m_CtrlStart0.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_0"), m_MainUIinEnglish));
	m_CtrlStart1.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_1"), m_MainUIinEnglish));
	m_CtrlStart2.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_2"), m_MainUIinEnglish));
	m_CtrlStart3.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_3"), m_MainUIinEnglish));
	m_CtrlStart4.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_4"), m_MainUIinEnglish));

	m_CtrlScore0_0.SetLabelUnit(_T(" "), _T(""));

	m_CtrlScore1_1.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_1_1"), m_MainUIinEnglish), _T(""));
	m_CtrlScore1_2.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_1_2"), m_MainUIinEnglish), _T(""));
	m_CtrlScore1_3.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_1_3"), m_MainUIinEnglish), _T(""));
	m_CtrlScore1_4.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_1_4"), m_MainUIinEnglish), _T(""));

	m_CtrlScore2_1.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_2_1"), m_MainUIinEnglish), _T(""));
	m_CtrlScore2_2.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_2_2"), m_MainUIinEnglish), _T(""));
	m_CtrlScore2_3.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_2_3"), m_MainUIinEnglish), _T(""));
	m_CtrlScore2_4.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_2_4"), m_MainUIinEnglish), _T(""));

	m_CtrlScore3_1.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_3_1"), m_MainUIinEnglish), _T(""));
	m_CtrlScore3_2.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_3_2"), m_MainUIinEnglish), _T(""));
	m_CtrlScore3_3.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_3_3"), m_MainUIinEnglish), _T(""));
	m_CtrlScore3_4.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_3_4"), m_MainUIinEnglish), _T(""));

	m_CtrlScore4_1.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_4_1"), m_MainUIinEnglish), _T(""));
	m_CtrlScore4_2.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_4_2"), m_MainUIinEnglish), _T(""));
	m_CtrlScore4_3.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_4_3"), m_MainUIinEnglish), _T(""));
	m_CtrlScore4_4.SetLabelUnit(i18n(_T("MainWindow"), _T("LABEL_4_4"), m_MainUIinEnglish), _T(""));

	m_CtrlSns1.SetToolTipText(i18n(_T("MainWindow"), _T("X_TOOLTIP"), m_MainUIinEnglish));
	m_CtrlQR.SetToolTipText(i18n(_T("MainWindow"), _T("QRCODE_TOOLTIP"), m_MainUIinEnglish));
	m_CtrlSubmit.SetWindowText(i18n(_T("MainWindow"), _T("SUBMIT"), m_MainUIinEnglish));
	m_CtrlSubmit.SetToolTipText(i18n(_T("MainWindow"), _T("SUBMIT_TOOLTIP"), m_MainUIinEnglish));

	Invalidate();

	WritePrivateProfileString(_T("Setting"), _T("Language"), LangName, m_Ini);

#ifdef SUISHO_SHIZUKU_SUPPORT
	m_VoiceVolume = GetPrivateProfileIntFx(_T("Setting"), _T("VoiceVolume"), 80, m_Ini);

	int id = ID_VOICE_VOLUME_080;

	switch (m_VoiceVolume)
	{
	case  0: id = ID_VOICE_VOLUME_000;	break;
	case 10: id = ID_VOICE_VOLUME_010;	break;
	case 20: id = ID_VOICE_VOLUME_020;	break;
	case 30: id = ID_VOICE_VOLUME_030;	break;
	case 40: id = ID_VOICE_VOLUME_040;	break;
	case 50: id = ID_VOICE_VOLUME_050;	break;
	case 60: id = ID_VOICE_VOLUME_060;	break;
	case 70: id = ID_VOICE_VOLUME_070;	break;
	case 80: id = ID_VOICE_VOLUME_080;	break;
	case 90: id = ID_VOICE_VOLUME_090;	break;
	case 100:id = ID_VOICE_VOLUME_100;	break;
	default: id = ID_VOICE_VOLUME_080;	break;
	}

	subMenu.Attach(menu->GetSubMenu(3)->GetSafeHmenu());
	cstr = i18n(_T("Menu"), _T("VOICE_VOLUME"));
	SUBMENU_MODIFY_MENU(3, MF_BYPOSITION, 3, cstr);
	subMenu.Detach();

	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, id, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
#endif

#ifdef SUISHO_AOI_SUPPORT
	TCHAR str[256];
	GetPrivateProfileStringFx(_T("Setting"), _T("VoiceLanguage"), _T(""), str, 256, m_Ini);
	CString voiceLanguage = str;

	if (cstr.IsEmpty()) // First Time
	{
		if (GetUserDefaultLCID() == 0x0411)// Japanese
		{
			GetPrivateProfileStringFx(_T("Setting"), _T("VoiceLanguage"), _T("Japanese"), str, 256, m_Ini);
			WritePrivateProfileStringFx(_T("Setting"), _T("VoiceLanguage"), _T("Japanese"), m_Ini);
		}
		else
		{
			GetPrivateProfileStringFx(_T("Setting"), _T("VoiceLanguage"), _T("English"), str, 256, m_Ini);
			WritePrivateProfileStringFx(_T("Setting"), _T("VoiceLanguage"), _T("English"), m_Ini);
		}
	}
	else
	{
		GetPrivateProfileStringFx(_T("Setting"), _T("VoiceLanguage"), _T("Japanese"), str, 256, m_Ini);
	}

	voiceLanguage = str;
	if (voiceLanguage.Find(_T("Japanese") == 0))
	{
		m_CurrentVoice = _T("Aoi-ja");
	}
	else
	{
		m_CurrentVoice = _T("Aoi-en");
	}

	subMenu.Attach(menu->GetSubMenu(3)->GetSafeHmenu());
	cstr = i18n(_T("Menu"), _T("VOICE_LANGUAGE"));
	SUBMENU_MODIFY_MENU(4, MF_BYPOSITION, 4, cstr);
	subMenu.Detach();

	if (voiceLanguage.Find(_T("Japanese") == 0))
	{
		menu->CheckMenuRadioItem(ID_VOICE_ENGLISH, ID_VOICE_JAPANESE, ID_VOICE_JAPANESE, MF_BYCOMMAND);
	}
	else
	{
		menu->CheckMenuRadioItem(ID_VOICE_ENGLISH, ID_VOICE_JAPANESE, ID_VOICE_ENGLISH, MF_BYCOMMAND);
	}
#endif
}

BOOL CCrystalMarkDlg::OnCommand(WPARAM wParam, LPARAM lParam) 
{
	// Select Theme
	if (WM_THEME_ID <= wParam && wParam < WM_THEME_ID + (UINT)m_MenuArrayTheme.GetSize())
	{
		CMenu menu;
		CMenu subMenu;
		menu.Attach(GetMenu()->GetSafeHmenu());
		subMenu.Attach(menu.GetSubMenu(MENU_THEME_INDEX)->GetSafeHmenu());

		m_CurrentTheme = m_MenuArrayTheme.GetAt(wParam - WM_THEME_ID);
		if (m_CurrentTheme.Compare(m_RandomThemeLabel) == 0)
		{
			m_CurrentTheme = GetRandomTheme();
			m_RandomThemeLabel = _T("Random");
			m_RandomThemeName = _T(" (") + m_CurrentTheme + _T(")");

			// ChangeTheme save the theme configuration to profile; so if we are on
			// Random, then save Random to profile.
			ChangeTheme(m_RandomThemeLabel);
		}
		else
		{
			ChangeTheme(m_MenuArrayTheme.GetAt(wParam - WM_THEME_ID));
			m_RandomThemeName = _T("");
		}

		SUBMENU_MODIFY_MENU(WM_THEME_ID, MF_STRING, WM_THEME_ID, m_RandomThemeLabel + m_RandomThemeName);
		subMenu.CheckMenuRadioItem(WM_THEME_ID, WM_THEME_ID + (UINT)m_MenuArrayTheme.GetSize(),
			(UINT)wParam, MF_BYCOMMAND);
		subMenu.Detach();
		menu.Detach();

		UpdateThemeInfo();
		UpdateDialogSize();

		return TRUE;
	}

	// Select Language
	if(WM_LANGUAGE_ID <= wParam && wParam < WM_LANGUAGE_ID + (UINT)m_MenuArrayLang.GetSize())
	{
		CMenu menu;
		CMenu subMenu;
		CMenu subMenuAN;
		CMenu subMenuOZ;
		menu.Attach(GetMenu()->GetSafeHmenu());
		subMenu.Attach(menu.GetSubMenu(MENU_LANG_INDEX)->GetSafeHmenu());
		subMenuAN.Attach(subMenu.GetSubMenu(0)->GetSafeHmenu());
		subMenuOZ.Attach(subMenu.GetSubMenu(1)->GetSafeHmenu());

		m_CurrentLang = m_MenuArrayLang.GetAt(wParam - WM_LANGUAGE_ID);
		m_CurrentLang.Replace(_T("9x"), _T(""));
		ChangeLang(m_CurrentLang);
		subMenuAN.CheckMenuRadioItem(WM_LANGUAGE_ID, WM_LANGUAGE_ID + (UINT)m_MenuArrayLang.GetSize(),
									(UINT)wParam, MF_BYCOMMAND);
		subMenuOZ.CheckMenuRadioItem(WM_LANGUAGE_ID, WM_LANGUAGE_ID + (UINT)m_MenuArrayLang.GetSize(),
									(UINT)wParam, MF_BYCOMMAND);

		subMenuOZ.Detach();
		subMenuAN.Detach();
		subMenu.Detach();
		menu.Detach();
	}

	return CMainDialogFx::OnCommand(wParam, lParam);
}

void CCrystalMarkDlg::OnZoom50()
{
	if (CheckRadioZoomType(ID_ZOOM_50, 50))
	{
		UpdateDialogSize();
	}
}

void CCrystalMarkDlg::OnZoom64()
{
	if (CheckRadioZoomType(ID_ZOOM_64, 64))
	{
		UpdateDialogSize();
	}
}


void CCrystalMarkDlg::OnZoom75()
{
	if (CheckRadioZoomType(ID_ZOOM_75, 75))
	{
		UpdateDialogSize();
	}
}

void CCrystalMarkDlg::OnZoom100()
{
	if (CheckRadioZoomType(ID_ZOOM_100, 100))
	{
		UpdateDialogSize();
	}
}

void CCrystalMarkDlg::OnZoom125()
{
	if (CheckRadioZoomType(ID_ZOOM_125, 125))
	{
		UpdateDialogSize();
	}
}

void CCrystalMarkDlg::OnZoom150()
{
	if (CheckRadioZoomType(ID_ZOOM_150, 150))
	{
		UpdateDialogSize();
	}
}

void CCrystalMarkDlg::OnZoom200()
{
	if (CheckRadioZoomType(ID_ZOOM_200, 200))
	{
		UpdateDialogSize();
	}
}

void CCrystalMarkDlg::OnZoom250()
{
	if (CheckRadioZoomType(ID_ZOOM_250, 250))
	{
		UpdateDialogSize();
	}
}

void CCrystalMarkDlg::OnZoom300()
{
	if (CheckRadioZoomType(ID_ZOOM_300, 300))
	{
		UpdateDialogSize();
	}
}

void CCrystalMarkDlg::OnZoomAuto()
{
	if (CheckRadioZoomType(ID_ZOOM_AUTO, 0))
	{
		UpdateDialogSize();
	}
}

BOOL CCrystalMarkDlg::CheckRadioZoomType(int id, int value)
{
	if(m_ZoomType == value)
	{
		return FALSE;
	}

	CMenu *menu = GetMenu();
	menu->CheckMenuRadioItem(ID_ZOOM_50, ID_ZOOM_AUTO, id, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();

	m_ZoomType = value;

	CString cstr;
	cstr.Format(_T("%d"), value);
	WritePrivateProfileString(_T("Setting"), _T("ZoomType"), cstr, m_Ini);

	ChangeZoomType(m_ZoomType);

	return TRUE;
}

void CCrystalMarkDlg::CheckRadioZoomType()
{
	int id = ID_ZOOM_AUTO;

	switch(m_ZoomType)
	{
	case  50: id = ID_ZOOM_50;	break;
	case  64: id = ID_ZOOM_64;	break;
	case  75: id = ID_ZOOM_75;	break;
	case 100: id = ID_ZOOM_100;	break;
	case 125: id = ID_ZOOM_125;	break;
	case 150: id = ID_ZOOM_150;	break;
	case 200: id = ID_ZOOM_200;	break;
	case 250: id = ID_ZOOM_250;	break;
	case 300: id = ID_ZOOM_300;	break;
	default:  id = ID_ZOOM_AUTO;break;
	}

	CMenu *menu = GetMenu();
	menu->CheckMenuRadioItem(ID_ZOOM_50, ID_ZOOM_AUTO, id, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
}

void CCrystalMarkDlg::OnHelp()
{
	if (GetUserDefaultLCID() == 0x0411) // Japanese
	{
		OpenUrl(URL_HELP_JA);
	}
	else // Other Language
	{
		OpenUrl(URL_HELP_EN);
	}
}

void CCrystalMarkDlg::OnCrystalDewWorld()
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

void CCrystalMarkDlg::OnCrystalMarkDB()
{
	if (GetUserDefaultLCID() == 0x0411)// Japanese
	{
		OpenUrl(URL_CRYSTALMARKDB_JA);
	}
	else // Other Language
	{
		OpenUrl(URL_CRYSTALMARKDB_EN);
	}
}

void CCrystalMarkDlg::OnFontSetting()
{
	CFontSelectionDlg fontSelection(this);
	if (fontSelection.DoModal() == IDOK)
	{
		m_FontFace = fontSelection.GetFontFace();
		m_FontScale = fontSelection.GetFontScale();
		m_FontRatio = m_FontScale / 100.0;
		m_FontRender = fontSelection.GetFontRender();

		CString cstr;
		WritePrivateProfileString(_T("Setting"), _T("FontFace"), _T("\"") + m_FontFace + _T("\""), m_Ini);
		cstr.Format(_T("%d"), m_FontScale);
		WritePrivateProfileString(_T("Setting"), _T("FontScale"), cstr, m_Ini);
		cstr.Format(_T("%d"), m_FontRender);
		WritePrivateProfileString(_T("Setting"), _T("FontRender"), cstr, m_Ini);

		UpdateDialogSize();
	}
}

void CCrystalMarkDlg::SetWindowTitle(CString message)
{
	CString title;

	if (!message.IsEmpty())
	{
		title.Format(_T("%s - %s"), PRODUCT_SHORT_NAME, (LPCTSTR)message);
	}
	else
	{
		title.Format(_T("%s %s %s"), PRODUCT_NAME, PRODUCT_VERSION, PRODUCT_EDITION);
	}

	if (m_AdminMode)
	{
		title += _T(" [Admin]");
	}

	SetWindowText(title);
}

void CCrystalMarkDlg::OnSD()
{
	AlertSound(VOICE(m_Score[0][0]), m_VoiceVolume);
}

void CCrystalMarkDlg::OnSubmit()
{
	CStringA urlA;
	CString url;

	urlA = GetRegisterUrl();
	if (urlA.IsEmpty())
	{
		return;
	}

#ifdef UNICODE	
	url = (LPCTSTR)UTF8toUTF16(urlA);
#else
	url = urlA;
#endif

#if _MSC_VER > 1310
	OpenUrl(url);
	if (! InternetCheckConnection(_T("https://crystalmark.info"), FLAG_ICC_FORCE_CONNECTION, 0))
	{
		if (AfxMessageBox(m_MesCopyClipboard, MB_OKCANCEL) == IDOK)
		{
			SetClipboardText(url);
		}
	}
#else
	if (AfxMessageBox(m_MesCopyClipboard, MB_OKCANCEL) == IDOK)
	{
		SetClipboardText(url);
	}
#endif
}

CString CCrystalMarkDlg::GetQRCodePath()
{
	return m_QRCodePath;
}

void CCrystalMarkDlg::OnQR()
{
	CStringA bodyA;
	bodyA = GetRegisterUrl();
	if (bodyA.IsEmpty())
	{
		return;
	}

	TCHAR tempPath[MAX_PATH];

	GetTempPath(MAX_PATH, tempPath);
	m_QRCodePath.Format(_T("%sqrcode-%03d.png"), tempPath, (int)(m_ZoomRatio * 100));

	int scale = 0;
	switch ((int)(m_ZoomRatio * 100))
	{
	case  50: scale = 2;  break;
	case  64: scale = 3;  break;
	case  75: scale = 3;  break;
	case 100: scale = 4;  break;
	case 125: scale = 5;  break;
	case 150: scale = 6;  break;
	case 200: scale = 8;  break;
	case 250: scale = 10; break;
	case 300: scale = 12; break;
	default:  scale = 4;  break;
	}

#ifdef OPTION_QR_CODE
	SaveQRCode(bodyA, m_QRCodePath, scale);
#endif

	CQRCodeDlg QRCodeDlg(this);
	QRCodeDlg.DoModal();

	DeleteFile(m_QRCodePath);
}

void CCrystalMarkDlg::OnTweet()
{
	Tweet();
}

void CCrystalMarkDlg::OnAds()
{
	if (GetUserDefaultLCID() == 0x0411) // Japanese
	{
		OpenUrl(URL_ADS_JA);
	}
	else // Other Language
	{
		OpenUrl(URL_ADS_EN);
	}
}

void CCrystalMarkDlg::ChangeControlStatus(BOOL status)
{
	if (status)
	{
		m_CtrlStart0.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_0"), m_MainUIinEnglish));
		m_CtrlStart1.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_1"), m_MainUIinEnglish));
		m_CtrlStart2.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_2"), m_MainUIinEnglish));
		m_CtrlStart3.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_3"), m_MainUIinEnglish));
		m_CtrlStart4.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_4"), m_MainUIinEnglish));

		m_CtrlSns1.EnableWindow(TRUE);
		if (m_Score[0][0] > 0)
		{
			m_CtrlQR.EnableWindow(TRUE);
			m_CtrlSubmit.EnableWindow(TRUE);
		}
	}
	else
	{
		m_CtrlStart0.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_STOP"), m_MainUIinEnglish));
		m_CtrlStart1.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_STOP"), m_MainUIinEnglish));
		m_CtrlStart2.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_STOP"), m_MainUIinEnglish));
		m_CtrlStart3.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_STOP"), m_MainUIinEnglish));
		m_CtrlStart4.SetWindowText(i18n(_T("MainWindow"), _T("BUTTON_STOP"), m_MainUIinEnglish));

		m_CtrlSns1.EnableWindow(FALSE);
		m_CtrlQR.EnableWindow(FALSE);
		m_CtrlSubmit.EnableWindow(FALSE);
	}
}

void CCrystalMarkDlg::Stop()
{
	if (m_BenchStatus)
	{
		m_BenchStatus = FALSE;

		if (pi.hProcess != NULL)
		{
			TerminateProcess(pi.hProcess, 0);
		}
	}
	ChangeControlStatus(TRUE);
	EnableMenus();
}

void CCrystalMarkDlg::OnStart0()
{
	if (m_BenchStatus == FALSE)
	{
		UpdateData(TRUE);

		for (int i = 0; i <= 4; i++) {
			for (int j = 0; j <= 4; j++) {
				m_Score[i][j] = 0;				
			}
		}

		UpdateScore();
		m_BenchStatus = TRUE;
		m_WinThread = AfxBeginThread(ExecBenchmark0, (void*)this);
		if (m_WinThread == NULL)
		{
			m_BenchStatus = FALSE;
		}
		else
		{
			ChangeControlStatus(FALSE);
		}
		DisableMenus();
	}
	else
	{
		Stop();
	}
}

void CCrystalMarkDlg::OnStart1()
{
	if (m_BenchStatus == FALSE)
	{
		UpdateData(TRUE);

		for (int j = 0; j <= 4; j++) {
			m_Score[1][j] = 0;
		}

		UpdateScore();
		m_BenchStatus = TRUE;
		m_WinThread = AfxBeginThread(ExecBenchmark1, (void*)this);
		if (m_WinThread == NULL)
		{
			m_BenchStatus = FALSE;
		}
		else
		{
			ChangeControlStatus(FALSE);
		}
		DisableMenus();
	}
	else
	{
		Stop();
	}
}

void CCrystalMarkDlg::OnStart2()
{
	if (m_BenchStatus == FALSE)
	{
		UpdateData(TRUE);

		for (int j = 0; j <= 4; j++) {
			m_Score[2][j] = 0;
		}

		UpdateScore();
		m_BenchStatus = TRUE;
		m_WinThread = AfxBeginThread(ExecBenchmark2, (void*)this);
		if (m_WinThread == NULL)
		{
			m_BenchStatus = FALSE;
		}
		else
		{
			ChangeControlStatus(FALSE);
		}
		DisableMenus();
	}
	else
	{
		Stop();
	}
}

void CCrystalMarkDlg::OnStart3()
{
	if (m_BenchStatus == FALSE)
	{
		UpdateData(TRUE);

		for (int j = 0; j <= 4; j++) {
			m_Score[3][j] = 0;
		}

		UpdateScore();
		m_BenchStatus = TRUE;
		m_WinThread = AfxBeginThread(ExecBenchmark3, (void*)this);
		if (m_WinThread == NULL)
		{
			m_BenchStatus = FALSE;
		}
		else
		{
			ChangeControlStatus(FALSE);
		}
		DisableMenus();
	}
	else
	{
		Stop();
	}
}

void CCrystalMarkDlg::OnStart4()
{
	if (m_BenchStatus == FALSE)
	{
		UpdateData(TRUE);

		for (int j = 0; j <= 4; j++) {
			m_Score[4][j] = 0;
		}

		UpdateScore();
		m_BenchStatus = TRUE;
		m_WinThread = AfxBeginThread(ExecBenchmark4, (void*)this);
		if (m_WinThread == NULL)
		{
			m_BenchStatus = FALSE;
		}
		else
		{
			ChangeControlStatus(FALSE);
		}
		DisableMenus();
	}
	else
	{
		Stop();
	}
}

LRESULT CCrystalMarkDlg::OnUpdateScore(WPARAM wParam, LPARAM lParam)
{
	UpdateScore();
	return 0;
}

LRESULT CCrystalMarkDlg::OnExitBenchmark(WPARAM wParam, LPARAM lParam)
{
	CString screenInfo = _T("");
	GetScreenInfo(screenInfo, &m_RsScreenWidth, &m_RsScreenHeight, &m_RsScreenColor, m_RsScreenSmoothing);
	m_CtrlSystemInfo5.SetWindowText(screenInfo);
	m_ScreenInfo = screenInfo;

	ChangeControlStatus(TRUE);
	EnableMenus();

#ifdef SUISHO_SHIZUKU_SUPPORT
	if (m_Score[0][0] > 0)
	{
		m_CtrlSD.ReloadImage(SD(m_Score[0][0]), 1);
		AlertSound(VOICE(m_Score[0][0]), m_VoiceVolume);
	}
#endif

	return 0;
}

LRESULT CCrystalMarkDlg::OnStartBenchmark(WPARAM wParam, LPARAM lParam)
{
	ChangeControlStatus(FALSE);
	DisableMenus();

	return 0;
}

LRESULT CCrystalMarkDlg::OnSecretVoice(WPARAM wParam, LPARAM lParam)
{
	AlertSound(VOICE(-1), m_VoiceVolume);

	return 0;
}

LRESULT CCrystalMarkDlg::OnUpdateMessage(WPARAM wParam, LPARAM lParam)
{
	CString wstr = _T("");
	CString lstr = _T("");

	if (wParam != NULL)
	{
		wstr = *((CString*)wParam);
	}

	if (lParam != NULL)
	{
		lstr = *((CString*)lParam);
	}

	SetWindowTitle(wstr);
	return 0;
}
void CCrystalMarkDlg::SetMeterLinear(CButtonFx* control, int score, double ratio)
{
	double meterRatio = 0.0;

	if (score >= 1)
	{
		meterRatio = ratio * score;
	}
	else
	{
		meterRatio = 0.0;
	}

	control->SetMeter(TRUE, meterRatio);
}

void CCrystalMarkDlg::SetMeter(CButtonFx* control, int score, double ratio)
{
	double meterRatio = 0.0;

	if (score >= 1)
	{
		meterRatio = ratio * log10((double)score);
	}
	else
	{
		meterRatio = 0.0;
	}

	control->SetMeter(TRUE, meterRatio);
}

void CCrystalMarkDlg::OnMainUIinEnglish()
{
	CMenu* menu = GetMenu();
	if (m_MainUIinEnglish)
	{
		m_MainUIinEnglish = FALSE;
		menu->CheckMenuItem(ID_MAIN_UI_IN_ENGLISH, MF_UNCHECKED);
		WritePrivateProfileStringFx(_T("Setting"), _T("MainUIinEnglish"), _T("0"), m_Ini);
	}
	else
	{
		m_MainUIinEnglish = TRUE;
		menu->CheckMenuItem(ID_MAIN_UI_IN_ENGLISH, MF_CHECKED);
		WritePrivateProfileStringFx(_T("Setting"), _T("MainUIinEnglish"), _T("1"), m_Ini);
	}
	SetMenu(menu);
	DrawMenuBar();

	ChangeLang(m_CurrentLang);
}

#ifdef SUISHO_AOI_SUPPORT
void CCrystalMarkDlg::OnVoiceEnglish()
{
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_ENGLISH, ID_VOICE_JAPANESE, ID_VOICE_ENGLISH, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();

	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceLanguage"), _T("English"), m_Ini);

	m_CurrentVoice = _T("Aoi-en");
}

void CCrystalMarkDlg::OnVoiceJapanese()
{
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_ENGLISH, ID_VOICE_JAPANESE, ID_VOICE_JAPANESE, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();

	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceLanguage"), _T("Japanese"), m_Ini);

	m_CurrentVoice = _T("Aoi-ja");
}
#endif

#ifdef SUISHO_SHIZUKU_SUPPORT
void CCrystalMarkDlg::OnVoiceVolume000()
{
	m_VoiceVolume = 0;
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, ID_VOICE_VOLUME_000, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceVolume"), _T("0"), m_Ini);
}

void CCrystalMarkDlg::OnVoiceVolume010()
{
	m_VoiceVolume = 10;
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, ID_VOICE_VOLUME_010, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceVolume"), _T("10"), m_Ini);
}

void CCrystalMarkDlg::OnVoiceVolume020()
{
	m_VoiceVolume = 20;
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, ID_VOICE_VOLUME_020, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceVolume"), _T("20"), m_Ini);
}

void CCrystalMarkDlg::OnVoiceVolume030()
{
	m_VoiceVolume = 30;
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, ID_VOICE_VOLUME_030, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceVolume"), _T("30"), m_Ini);
}

void CCrystalMarkDlg::OnVoiceVolume040()
{
	m_VoiceVolume = 40;
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, ID_VOICE_VOLUME_040, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceVolume"), _T("40"), m_Ini);
}

void CCrystalMarkDlg::OnVoiceVolume050()
{
	m_VoiceVolume = 50;
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, ID_VOICE_VOLUME_050, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceVolume"), _T("50"), m_Ini);
}

void CCrystalMarkDlg::OnVoiceVolume060()
{
	m_VoiceVolume = 60;
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, ID_VOICE_VOLUME_060, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceVolume"), _T("60"), m_Ini);
}

void CCrystalMarkDlg::OnVoiceVolume070()
{
	m_VoiceVolume = 70;
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, ID_VOICE_VOLUME_070, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceVolume"), _T("70"), m_Ini);
}

void CCrystalMarkDlg::OnVoiceVolume080()
{
	m_VoiceVolume = 80;
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, ID_VOICE_VOLUME_080, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceVolume"), _T("80"), m_Ini);
}

void CCrystalMarkDlg::OnVoiceVolume090()
{
	m_VoiceVolume = 90;
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, ID_VOICE_VOLUME_090, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceVolume"), _T("90"), m_Ini);
}

void CCrystalMarkDlg::OnVoiceVolume100()
{
	m_VoiceVolume = 100;
	CMenu* menu = GetMenu();
	menu->CheckMenuRadioItem(ID_VOICE_VOLUME_000, ID_VOICE_VOLUME_100, ID_VOICE_VOLUME_100, MF_BYCOMMAND);
	SetMenu(menu);
	DrawMenuBar();
	WritePrivateProfileStringFx(_T("Setting"), _T("VoiceVolume"), _T("100"), m_Ini);
}
#endif

////------------------------------------------------
//   QR Code
////------------------------------------------------

#include <atlimage.h>
#include "qrcodegen.h"

void SetPixels(CImage& image, int x, int y, int scale, bool isBlack) {
	for (int dy = 0; dy < scale; dy++) {
		for (int dx = 0; dx < scale; dx++) {
			image.SetPixelRGB(x * scale + dx, y * scale + dy, isBlack ? 0 : 255, isBlack ? 0 : 255, isBlack ? 0 : 255);
		}
	}
}

int CCrystalMarkDlg::SaveQRCode(CStringA& text, CString& fileName, int scale)
{
	enum qrcodegen_Ecc errCorLvl = qrcodegen_Ecc_LOW;

	uint8_t qrcode[qrcodegen_BUFFER_LEN_MAX];
	uint8_t tempBuffer[qrcodegen_BUFFER_LEN_MAX];
	bool success = qrcodegen_encodeText(text, tempBuffer, qrcode, errCorLvl, qrcodegen_VERSION_MIN, qrcodegen_VERSION_MAX, qrcodegen_Mask_AUTO, true);

	if (!success) {
		return 1;
	}

	int size = qrcodegen_getSize(qrcode);
	int imageSize = size * scale;

	CImage image;
	image.Create(imageSize, imageSize, 24);

	for (int y = 0; y < imageSize; y++) {
		for (int x = 0; x < imageSize; x++) {
			image.SetPixelRGB(x, y, 255, 255, 255);
		}
	}

	for (int y = 0; y < size; y++) {
		for (int x = 0; x < size; x++) {
			bool isBlack = qrcodegen_getModule(qrcode, x, y);
			SetPixels(image, x, y, scale, isBlack);
		}
	}

	HRESULT hr = image.Save(fileName, Gdiplus::ImageFormatPNG);
	if (FAILED(hr)) {
		return 1;
	}

	return 0;
}