/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

#pragma once

#include "Resource.h"

class CCrystalMarkApp : public CWinApp
{
public:
	CCrystalMarkApp();

public:
	virtual BOOL InitInstance();


	DECLARE_MESSAGE_MAP()
};

extern CCrystalMarkApp theApp;