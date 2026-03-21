/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

#include "stdafx.h"
#include "CrystalMark.h"
#include "CrystalMarkDlg.h"
#include "CrystalMark3DDlg.h"

#include "ExecBench.h"

#include <math.h>
#include <afxinet.h>
#pragma comment(lib, "wininet.lib")

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

UINT(*ExecBenchmark0)(LPVOID) = ExecBenchmarkAll;
UINT(*ExecBenchmark1)(LPVOID) = ExecBenchmarkScene1;
UINT(*ExecBenchmark2)(LPVOID) = ExecBenchmarkScene2;
UINT(*ExecBenchmark3)(LPVOID) = ExecBenchmarkScene3;
UINT(*ExecBenchmark4)(LPVOID) = ExecBenchmarkScene4;

CCrystalMark3DDlg::CCrystalMark3DDlg(CWnd* pParent /*=NULL*/)
{
	
}

CCrystalMark3DDlg::~CCrystalMark3DDlg()
{

}

typedef int(WINAPI* FuncGetSystemMetricsForDpi) (int nIndex, UINT dpi);
typedef UINT(WINAPI* FuncGetDpiForWindow) (HWND hWnd);

void CCrystalMark3DDlg::UpdateDialogSize()
{
	CDialogFx::UpdateDialogSize();

	int offsetX = 0;
	int offsetY = 0;
	int logoOffsetX = 0;
	int logoOffsetY = 0;
#ifdef SUISHO_SHIZUKU_SUPPORT
	if (m_CharacterPosition == 0)
	{
		offsetX = OFFSET_X;
	}
#else
#if _MSC_VER <= 1310
	offsetY = -72;
	logoOffsetX = 416;
	logoOffsetY = -8;
#endif
#endif

	ShowWindow(SW_HIDE);

	m_SizeX = SIZE_X;
	int y = GetPrivateProfileInt(_T("Setting"), _T("Height"), INT_MIN, m_Ini);
	if (y > 0)
	{
		m_SizeY = y;
	}

	if (m_SizeY < SIZE_MIN_Y)
	{
		m_SizeY = SIZE_MIN_Y;
	}
	else if (m_SizeY > SIZE_MAX_Y)
	{
		m_SizeY = SIZE_MAX_Y;
	}

	SetClientSize(m_SizeX, m_SizeY, m_ZoomRatio);
	if (m_hPal) { DeleteObject(m_hPal); m_hPal = NULL; }
	UpdateBackground(TRUE, FALSE);
	SetControlFont();

	////
	//// InitControl
	////
	m_CtrlStart0.InitControl(8 + offsetX, 208 + offsetY, 128, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Button")), 3, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlStart1.InitControl(8 + offsetX, 280 + offsetY, 128, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Button")), 3, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlStart2.InitControl(8 + offsetX, 352 + offsetY, 128, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Button")), 3, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlStart3.InitControl(8 + offsetX, 424 + offsetY, 128, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Button")), 3, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlStart4.InitControl(8 + offsetX, 496 + offsetY, 128, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Button")), 3, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);

	m_CtrlScore0_0.InitControl(144 + offsetX, 208 + offsetY, 408, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("MeterTotal")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);

	m_CtrlScore1_0.InitControl(144 + offsetX, 208 + offsetY, 0, 0, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore2_0.InitControl(352 + offsetX, 208 + offsetY, 0, 0, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore3_0.InitControl(560 + offsetX, 208 + offsetY, 0, 0, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore4_0.InitControl(768 + offsetX, 208 + offsetY, 0, 0, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);

	m_CtrlScore1_1.InitControl(144 + offsetX, 280 + offsetY, 408, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("MeterTotal")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore1_2.InitControl(560 + offsetX, 280 + offsetY, 200, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Meter")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore1_3.InitControl(768 + offsetX, 280 + offsetY, 200, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Meter")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore1_4.InitControl(768 + offsetX, 280 + offsetY,   0,  0, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);

	m_CtrlScore2_1.InitControl(144 + offsetX, 352 + offsetY, 408, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("MeterTotal")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore2_2.InitControl(560 + offsetX, 352 + offsetY, 200, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Meter")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore2_3.InitControl(768 + offsetX, 352 + offsetY, 200, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Meter")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore2_4.InitControl(768 + offsetX, 352 + offsetY,   0,  0, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);

	m_CtrlScore3_1.InitControl(144 + offsetX, 424 + offsetY, 408, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("MeterTotal")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore3_2.InitControl(560 + offsetX, 424 + offsetY, 200, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Meter")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore3_3.InitControl(768 + offsetX, 424 + offsetY, 200, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Meter")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore3_4.InitControl(768 + offsetX, 424 + offsetY,   0,  0, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);

	m_CtrlScore4_1.InitControl(144 + offsetX, 496 + offsetY, 408, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("MeterTotal")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore4_2.InitControl(560 + offsetX, 496 + offsetY, 200, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Meter")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore4_3.InitControl(768 + offsetX, 496 + offsetY, 200, 64, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Meter")), 2, BS_RIGHT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlScore4_4.InitControl(768 + offsetX, 496 + offsetY,   0,  0, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);

	m_CtrlComment.SetGlassColor(m_Glass, m_GlassAlpha);

#if _MSC_VER > 1310
#ifdef SUISHO_AOI_SUPPORT
	m_CtrlComment.InitControl(8 + offsetX, 560, 752, 56 + m_SizeY - SIZE_MIN_Y, m_ZoomRatio, &m_BkDC, IP(_T("Comment")), 1, ES_LEFT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE, FALSE);
#else
	m_CtrlComment.InitControl(8 + offsetX, 568 + offsetY, 752, 40 + m_SizeY - SIZE_MIN_Y, m_ZoomRatio, &m_BkDC, IP(_T("Comment")), 1, ES_LEFT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE, FALSE);
#endif
	m_CtrlComment.SetMargins((UINT)(m_MarginCommentLeft * m_ZoomRatio), (UINT)(m_MarginCommentRight * m_ZoomRatio));
	m_CtrlComment.Adjust();
#else
#ifdef UNICODE
#ifdef SUISHO_AOI_SUPPORT
	if (IsNT3())
	{
		m_CtrlComment.InitControl(8 + offsetX, 568 + offsetY, 752, 40 + m_SizeY - SIZE_MIN_Y, m_ZoomRatio, &m_BkDC, NULL, 0, ES_LEFT, SystemDraw, m_bHighContrast, FALSE, FALSE, FALSE);
		m_CtrlComment.SetDrawFrame(TRUE);
		m_CtrlComment.SetMargins((UINT)(m_MarginCommentLeft * m_ZoomRatio), (UINT)(m_MarginCommentRight * m_ZoomRatio));
	}
	else
	{
		m_CtrlCommentUpper.InitControl(8 + offsetX, 560 + offsetY, 752, 16 + m_SizeY - SIZE_MIN_Y, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("CommentU")), 1, SS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
		m_CtrlComment.InitControl(8 + offsetX, 576 + offsetY, 752, 40 + m_SizeY - SIZE_MIN_Y, m_ZoomRatio, &m_BkDC, IP(_T("CommentL")), 1, ES_LEFT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE, FALSE);
		m_CtrlComment.SetMargins((UINT)(m_MarginCommentLeft * m_ZoomRatio), (UINT)(m_MarginCommentRight * m_ZoomRatio));
	}
#else
	if (IsNT3())
	{
		m_CtrlComment.InitControl(8 + offsetX, 568 + offsetY, 752, 40 + m_SizeY - SIZE_MIN_Y, m_ZoomRatio, &m_BkDC, NULL, 0, ES_LEFT, SystemDraw, m_bHighContrast, FALSE, FALSE, FALSE);
		m_CtrlComment.SetDrawFrame(TRUE);
		m_CtrlComment.SetMargins((UINT)(m_MarginCommentLeft* m_ZoomRatio), (UINT)(m_MarginCommentRight* m_ZoomRatio));
	}
	else
	{
		m_CtrlCommentUpper.InitControl(8 + offsetX, 568 + offsetY, 752, 8 + m_SizeY - SIZE_MIN_Y, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("CommentU")), 1, SS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
		m_CtrlComment.InitControl(8 + offsetX, 576 + offsetY, 752, 32 + m_SizeY - SIZE_MIN_Y, m_ZoomRatio, &m_BkDC, IP(_T("CommentL")), 1, ES_LEFT, OwnerDrawImage, m_bHighContrast, FALSE, FALSE, FALSE);
		m_CtrlComment.SetMargins((UINT)(m_MarginCommentLeft * m_ZoomRatio), (UINT)(m_MarginCommentRight * m_ZoomRatio));
	}
#endif
#else
	m_CtrlComment.InitControl(8 + offsetX, 568 + offsetY, 752, 40 + m_SizeY - SIZE_MIN_Y, m_ZoomRatio, &m_BkDC, NULL, 0, ES_LEFT, SystemDraw, m_bHighContrast, FALSE, FALSE, FALSE);
	m_CtrlComment.SetDrawFrame(TRUE);
	m_CtrlComment.SetMargin(0, 8, 0, 8, m_ZoomRatio);
#endif
#endif

	m_CtrlSns1.InitControl(928 + offsetX, 568 + offsetY, 40, 40, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("X")), 3, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlQR.InitControl(880 + offsetX, 568 + offsetY, 40, 40, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("QR")), 3, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);

	m_CtrlSubmit.InitControl(768 + offsetX, 568 + offsetY, 104, 40, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("ButtonMini")), 5, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);

#ifdef SUISHO_SHIZUKU_SUPPORT
	m_CtrlSD.InitControl(8 + offsetX, 8, 128, 192, m_ZoomRatio, m_hPal, &m_BkDC, SD(m_Score[0][0]), 1, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
#endif

#if _MSC_VER > 1310
	m_CtrlAds.InitControl(560 + offsetX, 112, 408, 160, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("Ads")), 1, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);
	m_CtrlAds.ModifyStyle(WS_TABSTOP, 0);
//	m_CtrlAds.ShowWindow(SW_HIDE);
#endif

#ifdef SUISHO_SHIZUKU_SUPPORT
	m_CtrlCrystalMark.InitControl(144 + offsetX, 152, 408, 48, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("CrystalMark")), 1, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);

	m_LabelSystemInfo1.InitControl(144 + offsetX, 8, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_LabelSystemInfo2.InitControl(144 + offsetX, 32, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_LabelSystemInfo3.InitControl(144 + offsetX, 56, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_LabelSystemInfo4.InitControl(144 + offsetX, 80, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_LabelSystemInfo5.InitControl(144 + offsetX, 104, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_LabelSystemInfo6.InitControl(144 + offsetX, 128, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);

	m_CtrlSystemInfo1.InitControl(280 + offsetX, 8, 824, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo2.InitControl(280 + offsetX, 32, 824, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo3.InitControl(280 + offsetX, 56, 824, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo4.InitControl(280 + offsetX, 80, 824, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo5.InitControl(280 + offsetX, 104, 280, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo6.InitControl(280 + offsetX, 128, 280, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);

	m_CtrlGpuInfo.InitControl(280 + offsetX, 32, 688, 200, m_ZoomRatio, &m_BkDC, NULL, 0, ES_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, m_ComboBk, m_ComboBkSelected, m_Glass, m_GlassAlpha);

#else
	m_CtrlCrystalMark.InitControl(144 + offsetX + logoOffsetX, 152 + logoOffsetY, 408, 48, m_ZoomRatio, m_hPal, &m_BkDC, IP(_T("CrystalMark")), 1, BS_CENTER, OwnerDrawImage, m_bHighContrast, FALSE, FALSE);

	m_LabelSystemInfo1.InitControl(8 + offsetX, 8, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_LabelSystemInfo2.InitControl(8 + offsetX, 32, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_LabelSystemInfo3.InitControl(8 + offsetX, 56, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_LabelSystemInfo4.InitControl(8 + offsetX, 80, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);

	m_CtrlSystemInfo1.InitControl(144 + offsetX, 8, 824, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo2.InitControl(144 + offsetX, 32, 824, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo3.InitControl(144 + offsetX, 56, 824, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo4.InitControl(144 + offsetX, 80, 824, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);

#if _MSC_VER > 1310
	m_LabelSystemInfo5.InitControl(8 + offsetX, 104, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_LabelSystemInfo6.InitControl(8 + offsetX, 128, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo5.InitControl(144 + offsetX, 104, 408, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo6.InitControl(144 + offsetX, 128, 408, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
#else
	m_LabelSystemInfo5.InitControl(8 + offsetX, 104, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo5.InitControl(144 + offsetX, 104, 408, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_LabelSystemInfo6.InitControl(552 + offsetX, 104, 128, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_RIGHT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
	m_CtrlSystemInfo6.InitControl(688 + offsetX, 104, 280, 24, m_ZoomRatio, m_hPal, &m_BkDC, NULL, 0, BS_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, FALSE);
#endif

	m_CtrlGpuInfo.InitControl(144 + offsetX, 32, 824, 500, m_ZoomRatio, &m_BkDC, NULL, 0, ES_LEFT, OwnerDrawTransparent, m_bHighContrast, FALSE, m_ComboBk, m_ComboBkSelected, m_Glass, m_GlassAlpha);

#endif

#if _MSC_VER > 1310
	COMBOBOXINFO info = { 0 };
	info.cbSize = sizeof(COMBOBOXINFO);
	m_CtrlGpuInfo.GetComboBoxInfo(&info);
	SetLayeredWindow(info.hwndList, m_ComboAlpha);
#endif

	m_CtrlGpuInfo.SetMargin(0, 4, 0, 0, m_ZoomRatio);

	m_CtrlScore0_0.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore1_0.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore2_0.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore3_0.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore4_0.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);

	m_CtrlScore1_1.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore1_2.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore1_3.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore1_4.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore2_1.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore2_2.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore2_3.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore2_4.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore3_1.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore3_2.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore3_3.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore3_4.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore4_1.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore4_2.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore4_3.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);
	m_CtrlScore4_4.SetMargin(m_MarginMeterTop, m_MarginMeterLeft, m_MarginMeterBottom, m_MarginMeterRight, m_ZoomRatio);

	m_CtrlScore0_0.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);

	m_CtrlScore1_1.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore1_2.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore1_3.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore1_4.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);

	m_CtrlScore2_1.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore2_2.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore2_3.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore2_4.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);

	m_CtrlScore3_1.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore3_2.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore3_3.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore3_4.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);

	m_CtrlScore4_1.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore4_2.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore4_3.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore4_4.SetLabelUnitFormat(DT_LEFT | DT_TOP | DT_SINGLELINE, DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);

	m_CtrlScore0_0.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore1_0.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore2_0.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore3_0.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore4_0.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);

	m_CtrlScore1_1.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore1_2.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore1_3.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore1_4.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);

	m_CtrlScore2_1.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore2_2.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore2_3.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore2_4.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);

	m_CtrlScore3_1.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore3_2.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore3_3.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore3_4.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);

	m_CtrlScore4_1.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore4_2.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore4_3.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);
	m_CtrlScore4_4.SetTextFormat(DT_RIGHT | DT_BOTTOM | DT_SINGLELINE);

	m_CtrlCrystalMark.ModifyStyle(WS_TABSTOP, 0);

	m_LabelSystemInfo1.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_LabelSystemInfo2.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_LabelSystemInfo3.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_LabelSystemInfo4.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_LabelSystemInfo5.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_LabelSystemInfo6.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);

	m_CtrlSystemInfo1.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlSystemInfo2.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlSystemInfo3.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlSystemInfo4.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlSystemInfo5.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlSystemInfo6.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);

	m_CtrlScore0_0.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore1_0.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore2_0.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore3_0.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore4_0.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);

	m_CtrlScore1_1.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore1_2.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore1_3.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore1_4.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);

	m_CtrlScore2_1.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore2_2.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore2_3.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore2_4.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);

	m_CtrlScore3_1.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore3_2.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore3_3.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore3_4.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);

	m_CtrlScore4_1.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore4_2.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore4_3.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);
	m_CtrlScore4_4.ModifyStyle(WS_TABSTOP | BS_NOTIFY, 0);

	m_CtrlStart0.SetHandCursor();
	m_CtrlStart1.SetHandCursor();
	m_CtrlStart2.SetHandCursor();
	m_CtrlStart3.SetHandCursor();
	m_CtrlStart4.SetHandCursor();

	m_CtrlSns1.SetHandCursor();
	m_CtrlQR.SetHandCursor();
	m_CtrlSubmit.SetHandCursor();

#ifdef SUISHO_AOI_SUPPORT
	m_CtrlSD.SetHandCursor();
#endif
#if _MSC_VER > 1310
	m_CtrlAds.SetHandCursor();
#endif

	if (m_Score[0][0] == 0)
	{
		m_CtrlQR.EnableWindow(FALSE);
		m_CtrlSubmit.EnableWindow(FALSE);
	}

#ifdef SUISHO_SHIZUKU_SUPPORT
	m_CtrlSD.SetWindowText(_T(""));
#endif

#if _MSC_VER > 1310
	m_CtrlAds.SetWindowText(_T(""));
#endif

	// m_CtrlScore1_3.ShowWindow(SW_HIDE);
	// m_CtrlScore1_4.ShowWindow(SW_HIDE);

	UpdateScore();

	Invalidate();

	ShowWindow(SW_SHOW);
}

void CCrystalMark3DDlg::SetControlFont()
{
	m_CtrlStart0.SetFontEx(m_FontFace, 32, 24, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlStart1.SetFontEx(m_FontFace, 32, 24, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlStart2.SetFontEx(m_FontFace, 32, 24, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlStart3.SetFontEx(m_FontFace, 32, 24, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlStart4.SetFontEx(m_FontFace, 32, 24, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);

	m_CtrlScore0_0.SetFontEx(m_FontFace, 48, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);

	m_CtrlScore1_0.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore2_0.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore3_0.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore4_0.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);

	m_CtrlScore1_1.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore1_2.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore1_3.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore1_4.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);

	m_CtrlScore2_1.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore2_2.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore2_3.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore2_4.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);

	m_CtrlScore3_1.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore3_2.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore3_3.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore3_4.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);

	m_CtrlScore4_1.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore4_2.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore4_3.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlScore4_4.SetFontEx(m_FontFace, 32, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);

	m_CtrlComment.SetFontEx(m_FontFace, 20, 20, m_ZoomRatio, m_FontRatio, m_EditText, FW_BOLD);
#ifndef UNICODE
	m_CtrlComment.SetBkColor(m_EditBk);
#endif

	m_CtrlSns1.SetFontEx(m_FontFace, 24, 24, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	m_CtrlQR.SetFontEx(m_FontFace, 24, 24, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);

	m_CtrlSubmit.SetFontEx(m_FontFace, 20, 20, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	//	m_CtrlSettings.SetFontEx(m_FontFace, 20, 20, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
#ifdef SUISHO_SHIZUKU_SUPPORT
	m_CtrlSD.SetFontEx(m_FontFace, 24, 24, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
#endif
#if _MSC_VER > 1310
	m_CtrlAds.SetFontEx(m_FontFace, 24, 24, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
#endif


	if (ZoomType050 <= m_ZoomType && m_ZoomType <= ZoomType075)
	{
		m_LabelSystemInfo1.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_LabelSystemInfo2.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_LabelSystemInfo3.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_LabelSystemInfo4.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_LabelSystemInfo5.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_LabelSystemInfo6.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);

		m_CtrlSystemInfo1.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_CtrlSystemInfo2.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_CtrlSystemInfo3.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_CtrlSystemInfo4.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_CtrlSystemInfo5.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_CtrlSystemInfo6.SetFontEx(m_FontFace, 20, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	}
	else
	{
		m_LabelSystemInfo1.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_LabelSystemInfo2.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_LabelSystemInfo3.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_LabelSystemInfo4.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_LabelSystemInfo5.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_LabelSystemInfo6.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);

		m_CtrlSystemInfo1.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_CtrlSystemInfo2.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_CtrlSystemInfo3.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_CtrlSystemInfo4.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_CtrlSystemInfo5.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
		m_CtrlSystemInfo6.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ButtonText, FW_BOLD);
	}

	m_CtrlGpuInfo.SetFontEx(m_FontFace, 16, 16, m_ZoomRatio, m_FontRatio, m_ComboText, m_ComboTextSelected, FW_BOLD, m_FontRender);
	m_CtrlGpuInfo.SetItemHeightAll(24, m_ZoomRatio, m_FontRatio);
}

void CCrystalMark3DDlg::Tweet()
{
	CString url;
	CString total;
	CString scene1;
	CString scene2;
	CString scene3;
	CString scene4;
	CString comment;
	CStringA commentA;

	m_CtrlScore0_0.GetWindowText(total);
	m_CtrlScore1_1.GetWindowText(scene1);
	m_CtrlScore2_1.GetWindowText(scene2);
	m_CtrlScore3_1.GetWindowText(scene3);
	m_CtrlScore4_1.GetWindowText(scene4);
	m_CtrlComment.GetWindowText(comment);

	commentA = UE(comment);
#ifdef UNICODE	
	comment = (LPCTSTR)UTF8toUTF16(commentA);
#else
	comment = commentA;
#endif
	CString productName = PRODUCT_NAME; productName.Replace(_T(" "), _T("%20"));
	CString productVersion = PRODUCT_VERSION; productVersion.Replace(_T(" "), _T("%20"));
	url.Format(_T("https://x.com/intent/tweet?text=%s%%20%s%%0aTotal:%%20%d%%0aScene1:%%20%d%%0aScene2:%%20%d%%0aScene3:%%20%d%%0aScene4:%%20%d%%0a%s%%0a&hashtags=%s&url=%s"), productName.GetString(), productVersion.GetString(), _tstoi(total), _tstoi(scene1), _tstoi(scene2), _tstoi(scene3), _tstoi(scene4), comment.GetString(), X_POST_HASHTAGS, X_POST_URL);

#if _MSC_VER > 1310	
	AfxMessageBox(m_MesAttachScreenshotManually);
	OpenUrl(url);
	if (!InternetCheckConnection(_T("https://www.x.com"), FLAG_ICC_FORCE_CONNECTION, 0))
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

void CCrystalMark3DDlg::SaveText(CString fileName)
{
	CString cstr, clip;

	UpdateData(TRUE);

	clip = _T("\
------------------------------------------------------------------------------\r\n\
%PRODUCT% %VERSION%%EDITION% (C) %COPY_YEAR% %COPY_AUTHOR%\r\n\
                                  Crystal Dew World: https://crystalmark.info/\r\n\
------------------------------------------------------------------------------\r\n\
\
%SCORE%\
\r\n\
%SETTINGS%\
\r\n\
%SYSTEM%\
");

// %BENCHMARK%\
// \r\n\
// %OPENGL%\
// \r\n\



	clip.Replace(_T("%PRODUCT%"), PRODUCT_NAME);
	clip.Replace(_T("%VERSION%"), PRODUCT_VERSION);

	cstr = PRODUCT_EDITION;
	if (!cstr.IsEmpty())
	{
		clip.Replace(_T("%EDITION%"), _T(" ") PRODUCT_EDITION);
	}
	else
	{
		clip.Replace(_T("%EDITION%"), PRODUCT_EDITION);
	}
	clip.Replace(_T("%COPY_YEAR%"), PRODUCT_COPY_YEAR);
	clip.Replace(_T("%COPY_AUTHOR%"), PRODUCT_COPY_AUTHOR);

	CString mode = _T("");
	if (m_AdminMode) { mode += _T(" [Admin]"); }

	CString date = _T("");
	SYSTEMTIME st;
	GetLocalTime(&st);
	date.Format(_T("%04d/%02d/%02d %02d:%02d:%02d"), st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond);

	CString comment = _T("");
	m_CtrlComment.GetWindowText(comment);

	// Score
	CString score;

	score.Format(L"\
-- Score ---------------------------------------------------------------------\r\n\
   Total: %I64d\r\n\
  Scene1: %I64d (Max FPS: %5.1f  Avg FPS: %5.1f)\r\n\
  Scene2: %I64d (Max FPS: %5.1f  Avg FPS: %5.1f)\r\n\
  Scene3: %I64d (Max FPS: %5.1f  Avg FPS: %5.1f)\r\n\
  Scene4: %I64d (Max FPS: %5.1f  Avg FPS: %5.1f)\r\n\
",
m_Score[0][0],
m_Score[1][1],
m_Score[1][2] / 10.0,
m_Score[1][3] / 10.0,
m_Score[2][1],
m_Score[2][2] / 10.0,
m_Score[2][3] / 10.0,
m_Score[3][1],
m_Score[3][2] / 10.0,
m_Score[3][3] / 10.0,
m_Score[4][1],
m_Score[4][2] / 10.0,
m_Score[4][3] / 10.0
);

	clip.Replace(_T("%SCORE%"), score);

	// Settings
	CString settings;
	settings.Format(_T("\
-- Settings ------------------------------------------------------------------\r\n\
    Date: %s\r\n\
    Mode:%s\r\n\
 Comment: %s\r\n\
"),
(LPCTSTR)date,
(LPCTSTR)mode,
(LPCTSTR)comment
);
	clip.Replace(_T("%SETTINGS%"), settings);

	// System
	CString system;

	if (m_CtrlGpuInfo.IsWindowEnabled())
	{
		m_CtrlGpuInfo.GetWindowText(m_GpuInfo);
	}

	system.Format(_T("\
-- System --------------------------------------------------------------------\r\n\
     CPU: %s\r\n\
     GPU: %s\r\n\
  System: %s\r\n\
      OS: %s\r\n\
  Screen: %s\r\n\
  Memory: %s\r\n\
"),
(LPCTSTR)m_CpuInfo,
(LPCTSTR)m_GpuInfo,
(LPCTSTR)m_SystemInfo,
(LPCTSTR)m_OsInfo,
(LPCTSTR)m_ScreenInfo,
(LPCTSTR)m_MemoryInfo
);
	clip.Replace(_T("%SYSTEM%"), system);

/*
	// OpenGL
	CString openGL;

	openGL.Format(_T("\
-- OpenGL --------------------------------------------------------------------\r\n\
 Version: %s\r\n\
  Vendor: %s\r\n\
Renderer: %s\r\n\
"),
(LPCTSTR)m_OpenGLVersion,
(LPCTSTR)m_OpenGLVendor,
(LPCTSTR)m_OpenGLRenderer
);

	clip.Replace(_T("%OPENGL%"), (LPCTSTR)openGL);

	// Benchmark version
	CString benchmark;

	benchmark.Format(_T("\
-- Benchmark Version ---------------------------------------------------------\r\n\
  Scene1: %s\r\n\
  Scene2: %s\r\n\
  Scene3: %s\r\n\
  Scene4: %s\r\n\
"),
(LPCTSTR)m_SceneVersion[0],
(LPCTSTR)m_SceneVersion[1],
(LPCTSTR)m_SceneVersion[2],
(LPCTSTR)m_SceneVersion[3]
);
	clip.Replace(_T("%BENCHMARK%"), (LPCTSTR)benchmark);
*/
	if (fileName.IsEmpty())
	{
		SetClipboardText(clip);
	}
	else
	{
		CT2A utf8(clip, CP_UTF8);

		CFile file;
		if (file.Open(fileName, CFile::modeCreate | CFile::modeWrite))
		{
			file.Write((char*)utf8, (UINT)strlen(utf8));
			file.Close();
		}
	}
}

CStringA CCrystalMark3DDlg::GetRegisterUrl()
{
	CString body;
	CStringA bodyA;

	CString cstr;
	if (m_CtrlGpuInfo.IsWindowEnabled())
	{
		m_CtrlGpuInfo.GetWindowText(cstr);
	}
	else
	{
		m_CtrlSystemInfo2.GetWindowText(cstr);
	}
	CStringArray m_GpuList;
	SplitCString(cstr, _T(" ["), m_GpuList);
	if (m_GpuList.GetCount() >= 2)
	{
		m_RsGpu = m_GpuList[0];
		m_RsGpuVram = _tstoi(m_GpuList[1]);
	}
	else if (m_GpuList.GetCount() == 1)
	{
		m_RsGpu = m_GpuList[0];
		m_RsGpuVram = 0;
	}
	else
	{
		m_RsGpu = _T("");
		m_RsGpuVram = 0;
	}

	CString comment = _T("");
	m_CtrlComment.GetWindowText(comment);

	bodyA.Format("\
cm_version=%s&\
cm_edition=%s&\
s00=%I64d&\
s11=%I64d&\
s12=%I64d&\
s13=%I64d&\
s14=%I64d&\
s21=%I64d&\
s22=%I64d&\
s23=%I64d&\
s24=%I64d&\
s31=%I64d&\
s32=%I64d&\
s33=%I64d&\
s34=%I64d&\
s41=%I64d&\
s42=%I64d&\
s43=%I64d&\
s44=%I64d&\
cpu=%s&\
clock=%d&\
core=%d&\
thread=%d&\
gpu=%s&\
vram=%d&\
model=%s&\
baseboard=%s&\
os_name=%s&\
os_version=%s&\
os_architecture=%s&\
width=%d&\
height=%d&\
color=%d&\
smoothing=%s&\
memory=%d&\
comment=%s\
",
(LPCSTR)UE(PRODUCT_VERSION), (LPCSTR)UE(PRODUCT_EDITION),
m_Score[0][0], m_Score[1][1], m_Score[1][2], m_Score[1][3], m_Score[1][4], m_Score[2][1], m_Score[2][2], m_Score[2][3], m_Score[2][4],
m_Score[3][1], m_Score[3][2], m_Score[3][3], m_Score[3][4], m_Score[4][1], m_Score[4][2], m_Score[4][3], m_Score[4][4],
(LPCSTR)UE(m_RsCpu),
m_RsCpuClock,
m_RsCpuCore,
m_RsCpuThread,
(LPCSTR)UE(m_RsGpu), m_RsGpuVram, (LPCSTR)UE(m_RsComputerSystem), (LPCSTR)UE(m_RsBaseBoard),
(LPCSTR)UE(m_RsOsName), (LPCSTR)UE(m_RsOsVersion), (LPCSTR)UE(m_RsOsArchitecture),
m_RsScreenWidth, m_RsScreenHeight, m_RsScreenColor, (LPCSTR)UE(m_RsScreenSmoothing),
m_RsMemorySize,
(LPCSTR)UE(comment)
);
	CStringA hashMD5 = MD5(MD5_SEACRET + bodyA);

	bodyA += "&hash=" + hashMD5;

	return REGISTER_URL + bodyA;
}

void CCrystalMark3DDlg::UpdateScore()
{
	CString cstr;

	m_Score[0][0] = (m_Score[1][1] + m_Score[2][1] + m_Score[3][1] + m_Score[4][1]) / 4;

//	m_Score[0][0] = (__int64)pow((double)m_Score[1][1] * (double)m_Score[2][1] * (double)m_Score[3][1] * (double)m_Score[4][1], 0.25);

	cstr.Format(_T("%I64d"), m_Score[0][0]);
	m_CtrlScore[0][0]->SetWindowText(cstr);
	SetMeter(m_CtrlScore[0][0], (int)(m_Score[0][0]), 0.2);
	for (int i = 1; i <= 4; i++)
	{
		for (int j = 0; j <= 1; j++)
		{
			cstr.Format(_T("%I64d"), m_Score[i][j]);
			m_CtrlScore[i][j]->SetWindowText(cstr);
			SetMeter(m_CtrlScore[i][j], (int)(m_Score[i][j]), 0.2);
		}
		for (int j = 2; j <= 4; j++)
		{
			cstr.Format(_T("%.1f"), m_Score[i][j] / 10.0);
			m_CtrlScore[i][j]->SetWindowText(cstr);
			SetMeterLinear(m_CtrlScore[i][j], (int)(m_Score[i][j]), 1.0 / 1000);
		}
	}
}
