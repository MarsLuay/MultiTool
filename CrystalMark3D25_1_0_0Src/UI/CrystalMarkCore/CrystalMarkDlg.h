/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

#pragma once

#include "AboutDlg.h"
#include "FontSelectionDlg.h"

#include "DialogFx.h"
#include "MainDialogFx.h"
#include "ButtonFx.h"
#include "StaticFx.h"
#include "EditFx.h"
#include "ComboBoxFx.h"
#include "ListCtrlFx.h"
#include "UtilityFx.h"
#include "OsInfoFx.h"
#include "SystemInfoFx.h"

class CCrystalMarkDlg : public CMainDialogFx
{
public:
	CCrystalMarkDlg(CWnd* pParent = NULL);
	~CCrystalMarkDlg();

	enum { IDD = IDD_MAIN };

#ifdef SUISHO_SHIZUKU_SUPPORT
	static const int SIZE_X = 1200;
	static const int SIZE_Y = 616;
	static const int SIZE_MIN_Y = 616;
	static const int SIZE_MAX_Y = 616;
	static const int OFFSET_X = 224;
#else
	static const int SIZE_X = 976;
#if _MSC_VER > 1310
	static const int SIZE_Y = 616;
	static const int SIZE_MIN_Y = 616;
	static const int SIZE_MAX_Y = 616;
#else
	static const int SIZE_Y = 544;
	static const int SIZE_MIN_Y = 544;
	static const int SIZE_MAX_Y = 544;
#endif
	static const int OFFSET_X = 0;
#endif
	void ChangeLang(CString LangName);
	// void UpdateDialogSize();
	void SetWindowTitle(CString message);
	// void SaveText(CString fileName);
	void SetMeterLinear(CButtonFx* control, int score, double ratio);
	void SetMeter(CButtonFx* control, int score, double ratio);
	CString GetQRCodePath();
	
	// Benchmark
	volatile CWinThread* m_WinThread;
	volatile BOOL m_BenchStatus;
	void UpdateThemeInfo();

	// CPU Information
	int m_Cores;
	int m_Threads;

	// System Information
	CString m_CpuInfo;
	CString m_GpuInfo;
	CString m_SystemInfo;
	CString m_OsInfo;
	CString m_ScreenInfo;
	CString m_MemoryInfo;

	// Ranking System
	CString m_RsCpu;
	int m_RsCpuClock;
	int m_RsCpuCore;
	int m_RsCpuThread;
	CString m_RsGpu;
	int m_RsGpuVram;
	CString m_RsComputerSystem;
	CString m_RsBaseBoard;
	CString m_RsOsName;
	CString m_RsOsVersion;
	CString m_RsOsArchitecture;
	int m_RsScreenWidth;
	int m_RsScreenHeight;
	int m_RsScreenColor;
	CString m_RsScreenSmoothing;
	int m_RsMemorySize;

	// Score
	__int64 m_Score[5][5];

	// Margin
	int m_MarginButtonTop;
	int m_MarginButtonLeft;
	int m_MarginButtonBottom;
	int m_MarginButtonRight;
	int m_MarginMeterTop;
	int m_MarginMeterLeft;
	int m_MarginMeterBottom;
	int m_MarginMeterRight;
	int m_MarginCommentTop;
	int m_MarginCommentLeft;
	int m_MarginCommentBottom;
	int m_MarginCommentRight;
	
	// Message
	CString m_MesStopBenchmark;
	CString m_MesExeFileNotFound;
	CString m_MesExeFileModified;
	CString m_MesAttachScreenshotManually;
	CString m_MesCopyClipboard;

protected:
	// Virtual Function
	virtual CStringA GetRegisterUrl() = 0;
	virtual void SaveText(CString fileName) = 0;
	virtual void SetControlFont() = 0;
	virtual void Tweet() = 0;
	virtual void UpdateScore() = 0;
	virtual void UpdateDialogSize() = 0;

	BOOL CheckRadioZoomType(int id, int value);
	void CheckRadioZoomType();
	void EnableMenus();
	void DisableMenus();

	void ChangeControlStatus(BOOL status);
	void Stop();

	CString SD(__int64 score);
	CString VOICE(__int64 score);

	BOOL CheckThemeEdition(CString name);
	void DoDataExchange(CDataExchange* pDX);
	BOOL OnInitDialog();
	void SetClientSize(int sizeX, int sizeY, double zoomRatio);
	void OnOK();
	void OnCancel();
	BOOL OnCommand(WPARAM wParam, LPARAM lParam);
	BOOL PreTranslateMessage(MSG* pMsg);
	LRESULT OnQueryEndSession(WPARAM wParam, LPARAM lParam);
	int SaveQRCode(CStringA& text, CString& fileName, int scale);

	DECLARE_MESSAGE_MAP()
	afx_msg void OnPaint();

	afx_msg HCURSOR OnQueryDragIcon();
	afx_msg LRESULT OnUpdateScore(WPARAM wParam, LPARAM lParam);
	afx_msg LRESULT OnUpdateMessage(WPARAM wParam, LPARAM lParam);
	afx_msg LRESULT OnExitBenchmark(WPARAM wParam, LPARAM lParam);
	afx_msg LRESULT OnStartBenchmark(WPARAM wParam, LPARAM lParam);
	afx_msg LRESULT OnSecretVoice(WPARAM wParam, LPARAM lParam);
	afx_msg void OnExit();
	afx_msg void OnCopy();
#if _MSC_VER > 1310
	afx_msg void OnSaveText();
	afx_msg void OnSaveImage();
#endif
	afx_msg void OnZoom50();
	afx_msg void OnZoom64();
	afx_msg void OnZoom75();
	afx_msg void OnZoom100();
	afx_msg void OnZoom125();
	afx_msg void OnZoom150();
	afx_msg void OnZoom200();
	afx_msg void OnZoom250();
	afx_msg void OnZoom300();
	afx_msg void OnZoomAuto();
	afx_msg void OnAbout();
	afx_msg void OnFontSetting();
	afx_msg void OnHelp();
	afx_msg void OnCrystalDewWorld();
	afx_msg void OnCrystalMarkDB();
	afx_msg void OnSD();
	afx_msg void OnSubmit();
	afx_msg void OnQR();
	afx_msg void OnTweet();
	afx_msg void OnAds();
	afx_msg void OnStart0();
	afx_msg void OnStart1();
	afx_msg void OnStart2();
	afx_msg void OnStart3();
	afx_msg void OnStart4();

	afx_msg void OnMainUIinEnglish();

#ifdef SUISHO_AOI_SUPPORT
	afx_msg void OnVoiceEnglish();
	afx_msg void OnVoiceJapanese();
#endif

#ifdef SUISHO_SHIZUKU_SUPPORT
	afx_msg void OnVoiceVolume000();
	afx_msg void OnVoiceVolume010();
	afx_msg void OnVoiceVolume020();
	afx_msg void OnVoiceVolume030();
	afx_msg void OnVoiceVolume040();
	afx_msg void OnVoiceVolume050();
	afx_msg void OnVoiceVolume060();
	afx_msg void OnVoiceVolume070();
	afx_msg void OnVoiceVolume080();
	afx_msg void OnVoiceVolume090();
	afx_msg void OnVoiceVolume100();
#endif

	HICON m_hIcon;
	HICON m_hIconMini;
	BOOL m_AdminMode;
	BOOL m_MainUIinEnglish;

	CButtonFx* m_CtrlScore[5][5];

	CAboutDlg* m_AboutDlg;

	CButtonFx m_CtrlStart0;
	CButtonFx m_CtrlStart1;
	CButtonFx m_CtrlStart2;
	CButtonFx m_CtrlStart3;
	CButtonFx m_CtrlStart4;

	CButtonFx m_CtrlScore0_0;
	CButtonFx m_CtrlScore1_0;
	CButtonFx m_CtrlScore2_0;
	CButtonFx m_CtrlScore3_0;
	CButtonFx m_CtrlScore4_0;

	CButtonFx m_CtrlScore1_1;
	CButtonFx m_CtrlScore1_2;
	CButtonFx m_CtrlScore1_3;
	CButtonFx m_CtrlScore1_4;

	CButtonFx m_CtrlScore2_1;
	CButtonFx m_CtrlScore2_2;
	CButtonFx m_CtrlScore2_3;
	CButtonFx m_CtrlScore2_4;

	CButtonFx m_CtrlScore3_1;
	CButtonFx m_CtrlScore3_2;
	CButtonFx m_CtrlScore3_3;
	CButtonFx m_CtrlScore3_4;

	CButtonFx m_CtrlScore4_1;
	CButtonFx m_CtrlScore4_2;
	CButtonFx m_CtrlScore4_3;
	CButtonFx m_CtrlScore4_4;

	CButtonFx m_LabelSystemInfo1;
	CButtonFx m_LabelSystemInfo2;
	CButtonFx m_LabelSystemInfo3;
	CButtonFx m_LabelSystemInfo4;
	CButtonFx m_LabelSystemInfo5;
	CButtonFx m_LabelSystemInfo6;

	CButtonFx m_CtrlSystemInfo1;
	CButtonFx m_CtrlSystemInfo2;
	CButtonFx m_CtrlSystemInfo3;
	CButtonFx m_CtrlSystemInfo4;
	CButtonFx m_CtrlSystemInfo5;
	CButtonFx m_CtrlSystemInfo6;

	CComboBoxFx m_CtrlGpuInfo;

#if _MSC_VER <= 1310
	CStaticFx m_CtrlCommentUpper;
#endif
	CEditFx m_CtrlComment;

	CButtonFx m_CtrlSns1;
	CButtonFx m_CtrlQR;
	CButtonFx m_CtrlCrystalMark;
	CButtonFx m_CtrlSubmit;
#ifdef SUISHO_SHIZUKU_SUPPORT
	CButtonFx m_CtrlSD;
#endif
	CButtonFx m_CtrlSettings;

#if _MSC_VER > 1310
	CButtonFx m_CtrlAds;
#endif

	CString m_QRCodePath;
};
