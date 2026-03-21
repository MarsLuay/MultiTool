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

#include <afxmt.h>
#include <winioctl.h>
#include <mmsystem.h>
#include <math.h>
#pragma comment(lib,"winmm.lib")

#pragma warning(disable : 4996)

static HANDLE hFile;

UINT ExecBenchmarkScene(LPVOID dlg, int sceneId, CString exe, CString option);
static UINT Exit(LPVOID dlg, BOOL forceExit);

#define BENCHMARK_SCENE1_32				_T("Resource\\Benchmark\\x86\\SceneX32.exe")
#define BENCHMARK_SCENE1_64				_T("Resource\\Benchmark\\x64\\SceneX64.exe")
#define BENCHMARK_SCENE1_ARM64			_T("Resource\\Benchmark\\ARM64\\SceneW2A64.exe")

#define BENCHMARK_SCENE2_32				_T("Resource\\Benchmark\\x86\\SceneX32.exe")
#define BENCHMARK_SCENE2_64				_T("Resource\\Benchmark\\x64\\SceneX64.exe")
#define BENCHMARK_SCENE2_ARM64			_T("Resource\\Benchmark\\ARM64\\SceneW2A64.exe")

#define BENCHMARK_SCENE3_32				_T("Resource\\Benchmark\\x86\\SceneX32.exe")
#define BENCHMARK_SCENE3_64				_T("Resource\\Benchmark\\x64\\SceneX64.exe")
#define BENCHMARK_SCENE3_ARM64			_T("Resource\\Benchmark\\ARM64\\SceneW2A64.exe")

#define BENCHMARK_SCENE4_32				_T("Resource\\Benchmark\\x86\\SceneW2X32.exe")
#define BENCHMARK_SCENE4_64				_T("Resource\\Benchmark\\x64\\SceneW2X64.exe")
#define BENCHMARK_SCENE4_ARM64			_T("Resource\\Benchmark\\ARM64\\SceneW2A64.exe")

#define BENCHMARK_SCENE1_OPTION			_T("LiveCodingVJ20231104.frag 30.0 10.0")
#define BENCHMARK_SCENE2_OPTION			_T("LiveCodingGam0022.frag 27.5 10.0")
#define BENCHMARK_SCENE3_OPTION			_T("stairway-to-sessions-by-kamoshika.frag 30.0 10.0")
#define BENCHMARK_SCENE4_OPTION			_T("marble-race-in-truchet-city-by-yue.frag 60.0 10.0")

PROCESS_INFORMATION pi = { 0 };
BOOL BenchmarkAll = FALSE;

int ExecAndWait(TCHAR *pszCmd, BOOL bNoWindow)
{
	DWORD Code = 0;
	BOOL bSuccess;
	STARTUPINFO si;

	memset(&si, 0, sizeof(STARTUPINFO));
	si.cb = sizeof(STARTUPINFO);

	if (bNoWindow) {
		si.dwFlags = STARTF_USESHOWWINDOW;
		si.wShowWindow = SW_HIDE;
	}

	bSuccess = CreateProcess(NULL, pszCmd, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);
	if (bSuccess != TRUE)
	{
		return 0;
	}

	WaitForInputIdle(pi.hProcess, INFINITE);
	WaitForSingleObject(pi.hProcess, INFINITE);
	GetExitCodeProcess(pi.hProcess, &Code);

	// CloseHandle(pi.hThread);
	CloseHandle(pi.hProcess);

	return Code;
}

int ExecsAndWait(CString exePath, BOOL bNoWindow, int testNo, int maxThreads)
{
	DWORD exitCode = 0;
	STARTUPINFO si;
	int score = 0;

	memset(&si, 0, sizeof(STARTUPINFO));
	si.cb = sizeof(STARTUPINFO);

	if (bNoWindow) {
		si.dwFlags = STARTF_USESHOWWINDOW;
		si.wShowWindow = SW_HIDE;
	}

	CString command;

	int maxProcesses = 1; //(maxThreads - 1) / 64 + 1;
	HANDLE* hProcess = new HANDLE[maxProcesses];
	PROCESS_INFORMATION* processInfo = new PROCESS_INFORMATION[maxProcesses];

	for (int i = 0; i < maxProcesses; i++)
	{
		command.Format(_T("\"%s\" %d %d"), (LPCTSTR)exePath, testNo, maxThreads / maxProcesses);

		if (CreateProcess(NULL, (LPTSTR)(LPCTSTR)command, NULL, NULL, FALSE, 0, NULL, NULL, &si, &processInfo[i]))
		{

		}
		hProcess[i] = processInfo[i].hProcess;
	}

	for (int i = 0; i < maxProcesses; i++)
	{
		WaitForInputIdle(hProcess[i], INFINITE);
	}

	WaitForMultipleObjects(maxProcesses, hProcess, TRUE, INFINITE);

	for (int i = 0; i < maxProcesses; i++)
	{
		if (hProcess[i] != NULL)
		{
			GetExitCodeProcess(hProcess[i], &exitCode);
			CloseHandle(hProcess[i]);

			score += exitCode;
		}
	}

	delete[] hProcess;
	delete[] processInfo;

	if (score == 0) { score = 1; }

	return score;
}

double geometricMean(double data[], int count)
{
	if (count <= 1) { return -1.0; }

	double value = data[0];
	for (int i = 1; i < count; i++)
	{
		value *= data[i];
	}

	value = pow(value, 1.0 / count);

	return value;
}

UINT ExecBenchmarkAll(LPVOID dlg)
{
	BenchmarkAll = TRUE;

	if (!ExecBenchmarkScene1(dlg)) { BenchmarkAll = FALSE; return FALSE; }
	if (!ExecBenchmarkScene2(dlg)) { BenchmarkAll = FALSE; return FALSE; }
	if (!ExecBenchmarkScene3(dlg)) { BenchmarkAll = FALSE; return FALSE; }
	if (!ExecBenchmarkScene4(dlg)) { BenchmarkAll = FALSE; return FALSE; }

	BenchmarkAll = FALSE;

	Exit(dlg, FALSE);
	return TRUE;
}

UINT ExecBenchmarkScene1(LPVOID dlg)
{
	CString exe;
#ifdef _M_ARM64
	exe = BENCHMARK_SCENE1_ARM64;
#elif _M_X64
	exe = BENCHMARK_SCENE1_64;
#else
	exe = BENCHMARK_SCENE1_32;
#endif
	return ExecBenchmarkScene(dlg, 1, exe, BENCHMARK_SCENE1_OPTION);
}

UINT ExecBenchmarkScene2(LPVOID dlg)
{
	CString exe;
#ifdef _M_ARM64
	exe = BENCHMARK_SCENE2_ARM64;
#elif _M_X64
	exe = BENCHMARK_SCENE2_64;
#else
	exe = BENCHMARK_SCENE2_32;
#endif
	return ExecBenchmarkScene(dlg, 2, exe, BENCHMARK_SCENE2_OPTION);
}

UINT ExecBenchmarkScene3(LPVOID dlg)
{
	CString exe;
#ifdef _M_ARM64
	exe = BENCHMARK_SCENE3_ARM64;
#elif _M_X64
	exe = BENCHMARK_SCENE3_64;
#else
	exe = BENCHMARK_SCENE3_32;
#endif
	return ExecBenchmarkScene(dlg, 3, exe, BENCHMARK_SCENE3_OPTION);
}

UINT ExecBenchmarkScene4(LPVOID dlg)
{
	CString exe;
#ifdef _M_ARM64
	exe = BENCHMARK_SCENE4_ARM64;
#elif _M_X64
	exe = BENCHMARK_SCENE4_64;
#else
	exe = BENCHMARK_SCENE4_32;
#endif
	return ExecBenchmarkScene(dlg, 4, exe, BENCHMARK_SCENE4_OPTION);
}

UINT ExecBenchmarkScene(LPVOID dlg, int sceneId, CString exe, CString option)
{
	CString command;
	CString exePath;

	TCHAR* ptrEnd;
	TCHAR path[MAX_PATH];
	::GetModuleFileName(NULL, path, MAX_PATH);
	if ((ptrEnd = _tcsrchr(path, '\\')) != NULL)
	{
		*ptrEnd = '\0';
	}

	exePath.Format(_T("%s\\%s"), path, exe.GetString());

	if (! CheckCodeSign(CERTNAME, exePath))
	{
		AfxMessageBox(((CCrystalMark3DDlg*)dlg)->m_MesExeFileModified);
		return Exit(dlg, TRUE);
	}

	if (!IsFileExist((LPCTSTR)exePath))
	{
		AfxMessageBox(((CCrystalMark3DDlg*)dlg)->m_MesExeFileNotFound);
		return Exit(dlg, TRUE);
	}

	command.Format(_T("\"%s\" %s"), (LPCTSTR)exePath, (LPCTSTR)option);

	DWORD Code = 0;
	BOOL bSuccess = FALSE;
	STARTUPINFO si = { 0 };

	memset(&si, 0, sizeof(STARTUPINFO));
	si.cb = sizeof(STARTUPINFO);

	CString name;
	name.Format(PRODUCT_SHORT_NAME);
	DWORD size = 1024 * sizeof(TCHAR);

	TCHAR result[1024] = { 0 };

	HANDLE hSharedMemory = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, NULL, size, (LPCTSTR)name);
	if (hSharedMemory != NULL)
	{
		TCHAR* pMemory = (TCHAR*)MapViewOfFile(hSharedMemory, FILE_MAP_ALL_ACCESS, NULL, NULL, size);
		if (pMemory != NULL)
		{
			bSuccess = CreateProcess(NULL, (TCHAR*)(LPCTSTR)command, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);
			if (bSuccess != TRUE)
			{
				UnmapViewOfFile(pMemory);
				CloseHandle(hSharedMemory);
				return Exit(dlg, TRUE);
			}

			WaitForInputIdle(pi.hProcess, INFINITE);
			WaitForSingleObject(pi.hProcess, INFINITE);
			GetExitCodeProcess(pi.hProcess, &Code);

			CloseHandle(pi.hThread);
			CloseHandle(pi.hProcess);

			pi.hProcess = NULL;

			RtlCopyMemory(result, pMemory, size);

			UnmapViewOfFile(pMemory);
			CloseHandle(hSharedMemory);
		}
	}

	// Read Score
	__int64 score[4] = { 0 };

	CString version;
	CString cstr;
	CString token;
	int curPos = 0;
	cstr = result;

	token = cstr.Tokenize(_T("\n"), curPos);
	while (token != _T(""))
	{
		CString leftPart = token.SpanExcluding(_T("="));
		CString rightPart = token.Mid(leftPart.GetLength() + 1);

		if (leftPart.Find(_T("Version")) == 0){ version = rightPart; }
		if (leftPart.Find(_T("Score")) == 0) { score[0] = _ttoi64(rightPart); }
		if (leftPart.Find(_T("Max")) == 0) { score[1] = _ttoi64(rightPart); }
		if (leftPart.Find(_T("Avg")) == 0) { score[2] = _ttoi64(rightPart); }
		if (leftPart.Find(_T("Min")) == 0) { score[3] = _ttoi64(rightPart); }

		token = cstr.Tokenize(_T("\n"), curPos);
	}

	if (!version.IsEmpty())
	{
		((CCrystalMark3DDlg*)dlg)->m_SceneVersion[sceneId - 1] = version;
	}
	else
	{
		Exit(dlg, TRUE);
		return FALSE;
	}

	((CCrystalMark3DDlg*)dlg)->m_Score[sceneId][0] = score[0];
	((CCrystalMark3DDlg*)dlg)->m_Score[sceneId][1] = score[0];
	((CCrystalMark3DDlg*)dlg)->m_Score[sceneId][2] = score[1];
	((CCrystalMark3DDlg*)dlg)->m_Score[sceneId][3] = score[2];
	((CCrystalMark3DDlg*)dlg)->m_Score[sceneId][4] = score[3];

//	if (((CCrystalMark3DDlg*)dlg)->m_Score[sceneId][1] == 0) { ((CCrystalMark3DDlg*)dlg)->m_Score[sceneId][1] = 1; }
	::PostMessage(((CCrystalMark3DDlg*)dlg)->GetSafeHwnd(), WM_UPDATE_SCORE, 0, 0);

	Exit(dlg, FALSE);

	return TRUE;
}

UINT Exit(LPVOID dlg, BOOL forceExit)
{
	static CString cstr;
	cstr = _T("");

	::PostMessage(((CCrystalMark3DDlg*)dlg)->GetSafeHwnd(), WM_UPDATE_MESSAGE, NULL, (LPARAM)&cstr);

	if (! BenchmarkAll || forceExit)
	{
		::PostMessage(((CCrystalMark3DDlg*)dlg)->GetSafeHwnd(), WM_EXIT_BENCHMARK, 0, 0);
		((CCrystalMark3DDlg*)dlg)->m_BenchStatus = FALSE;
		((CCrystalMark3DDlg*)dlg)->m_WinThread = NULL;
	}

	return 0;
}