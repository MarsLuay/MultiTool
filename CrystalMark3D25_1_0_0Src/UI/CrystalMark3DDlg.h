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
#include "CrystalMarkDlg.h"

class CCrystalMark3DDlg : public CCrystalMarkDlg
{
public:
	CCrystalMark3DDlg(CWnd* pParent = NULL);
	~CCrystalMark3DDlg();

	// Virtual Function
	virtual CStringA GetRegisterUrl();
	virtual void SaveText(CString fileName);
	virtual void SetControlFont();
	virtual void Tweet();
	virtual void UpdateScore();
	virtual void UpdateDialogSize();

	// Benchmark Client Version
	CString m_SceneVersion[4];
};


