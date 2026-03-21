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

class CQRCodeDlg : public CDialogFx
{
	DECLARE_DYNCREATE(CQRCodeDlg)

	static const int SIZE_X = 384;
	static const int SIZE_Y = 384;

public:
	CQRCodeDlg(CWnd* pParent = NULL);
	virtual ~CQRCodeDlg();

	enum { IDD = IDD_QR_CODE };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);
	virtual BOOL OnInitDialog();
	virtual void UpdateDialogSize();

	DECLARE_MESSAGE_MAP()

	CButtonFx m_CtrlQRCode;
	CString m_QRCodePath;
};
