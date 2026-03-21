/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

#pragma once
#include "DialogFx.h"
#include "StaticFx.h"
#include "ButtonFx.h"

class CAboutDlg : public CDialogFx
{
	DECLARE_DYNCREATE(CAboutDlg)

#ifdef CRYSTALMARK_3D
	static const int SIZE_X = 640;
	static const int SIZE_Y = 640;
#elif SUISHO_AOI_SUPPORT
	static const int SIZE_X = 640;
	static const int SIZE_Y = 640;
#elif SUISHO_SHIZUKU_SUPPORT
	static const int SIZE_X = 640;
	static const int SIZE_Y = 660;
#else
	static const int SIZE_X = 540;
	static const int SIZE_Y = 168;
#endif

public:
	CAboutDlg(CWnd* pParent = NULL);
	virtual ~CAboutDlg();

	enum { IDD = IDD_ABOUT };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);
	virtual BOOL OnInitDialog();
	virtual void UpdateDialogSize();

	DECLARE_MESSAGE_MAP()
	afx_msg void OnLogo();
	afx_msg void OnVersion();
	afx_msg void OnLicense();
	afx_msg void OnProjectSite1();
	afx_msg void OnProjectSite2();
	afx_msg void OnProjectSite3();
	afx_msg void OnProjectSite4();
	afx_msg void OnProjectSite5();

#ifdef CRYSTALMARK_3D
	afx_msg void OnProjectOwner1();
	afx_msg void OnProjectOwner2();
	afx_msg void OnProjectOwner3();
	afx_msg void OnProjectOwner4();
	afx_msg void OnProjectOwner5();
#endif

#ifdef SUISHO_SHIZUKU_SUPPORT
	afx_msg void OnSecretVoice();
	CButtonFx m_CtrlSecretVoice;
#endif
	CButtonFx m_CtrlLogo;
	CButtonFx m_CtrlProjectSite1;
	CButtonFx m_CtrlProjectSite2;
	CButtonFx m_CtrlProjectSite3;
	CButtonFx m_CtrlProjectSite4;
	CButtonFx m_CtrlProjectSite5;
	CButtonFx m_CtrlVersion;
	CButtonFx m_CtrlLicense;

	CButtonFx m_CtrlEdition;
	CButtonFx m_CtrlRelease;
	CButtonFx m_CtrlCopyright1;
	CButtonFx m_CtrlCopyright2;
	CButtonFx m_CtrlCopyright3;

#ifdef CRYSTALMARK_3D
	CButtonFx m_CtrlProjectOwner1;
	CButtonFx m_CtrlProjectOwner2;
	CButtonFx m_CtrlProjectOwner3;
	CButtonFx m_CtrlProjectOwner4;
	CButtonFx m_CtrlProjectOwner5;
#endif
};
