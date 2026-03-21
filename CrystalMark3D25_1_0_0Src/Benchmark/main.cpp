/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

#define NOMINMAX
#include <windows.h>
#include <gl/GL.h>
#pragma comment(lib, "opengl32.lib")
#pragma comment(lib, "gdi32.lib")
#pragma comment(lib, "user32.lib")

#include <string>
#include <vector>
#include <fstream>
#include <sstream>
#include <iomanip>
#include <filesystem>
#include <cwchar>
#include <chrono>
#include <cstdint>
#include <type_traits>
#include <algorithm>
#include <cmath>

#include <deque>

#include <bcrypt.h>
#include <array>
#include <cctype>
#include <cstdio>

// --------- GL guard typedefs ---------
#ifndef APIENTRY
#define APIENTRY __stdcall
#endif
#ifndef APIENTRYP
#define APIENTRYP APIENTRY *
#endif
#ifndef GLchar
typedef char GLchar;
#endif
#ifndef GLsizeiptr
#include <cstddef>
typedef ptrdiff_t GLsizeiptr;
#endif
#ifndef GLuint64
typedef unsigned long long GLuint64;
#endif

#ifndef GL_ARRAY_BUFFER
#define GL_ARRAY_BUFFER                   0x8892
#define GL_STATIC_DRAW                    0x88E4
#define GL_VERTEX_SHADER                  0x8B31
#define GL_FRAGMENT_SHADER                0x8B30
#define GL_COMPILE_STATUS                 0x8B81
#define GL_LINK_STATUS                    0x8B82
#define GL_INFO_LOG_LENGTH                0x8B84
#define GL_TIME_ELAPSED                   0x88BF
#define GL_QUERY_RESULT_AVAILABLE         0x8867
#define GL_QUERY_RESULT                   0x8866
#endif

// ---- WGL extensions for modern context ----
typedef HGLRC(APIENTRYP PFNWGLCREATECONTEXTATTRIBSARBPROC)(HDC, HGLRC, const int*);
typedef BOOL(APIENTRYP PFNWGLCHOOSEPIXELFORMATARBPROC)(HDC, const int*, const FLOAT*, UINT, int*, UINT*);
static PFNWGLCREATECONTEXTATTRIBSARBPROC pwglCreateContextAttribsARB = nullptr;
static PFNWGLCHOOSEPIXELFORMATARBPROC    pwglChoosePixelFormatARB = nullptr;

// ---- GL function pointers (minimal) ----
typedef GLuint(APIENTRYP PFNGLCREATEPROGRAMPROC)(void);
typedef GLuint(APIENTRYP PFNGLCREATESHADERPROC)(GLenum);
typedef void   (APIENTRYP PFNGLSHADERSOURCEPROC)(GLuint, GLsizei, const GLchar* const*, const GLint*);
typedef void   (APIENTRYP PFNGLCOMPILESHADERPROC)(GLuint);
typedef void   (APIENTRYP PFNGLGETSHADERIVPROC)(GLuint, GLenum, GLint*);
typedef void   (APIENTRYP PFNGLGETSHADERINFOLOGPROC)(GLuint, GLsizei, GLsizei*, GLchar*);
typedef void   (APIENTRYP PFNGLATTACHSHADERPROC)(GLuint, GLuint);
typedef void   (APIENTRYP PFNGLLINKPROGRAMPROC)(GLuint);
typedef void   (APIENTRYP PFNGLGETPROGRAMIVPROC)(GLuint, GLenum, GLint*);
typedef void   (APIENTRYP PFNGLGETPROGRAMINFOLOGPROC)(GLuint, GLsizei, GLsizei*, GLchar*);
typedef void   (APIENTRYP PFNGLUSEPROGRAMPROC)(GLuint);
typedef GLint(APIENTRYP PFNGLGETUNIFORMLOCATIONPROC)(GLuint, const GLchar*);
typedef void   (APIENTRYP PFNGLUNIFORM1FPROC)(GLint, GLfloat);
typedef void   (APIENTRYP PFNGLUNIFORM1IPROC)(GLint, GLint);
typedef void   (APIENTRYP PFNGLUNIFORM3FPROC)(GLint, GLfloat, GLfloat, GLfloat);
typedef void   (APIENTRYP PFNGLUNIFORM4FPROC)(GLint, GLfloat, GLfloat, GLfloat, GLfloat);
typedef void   (APIENTRYP PFNGLGENBUFFERSPROC)(GLsizei, GLuint*);
typedef void   (APIENTRYP PFNGLBINDBUFFERPROC)(GLenum, GLuint);
typedef void   (APIENTRYP PFNGLBUFFERDATAPROC)(GLenum, GLsizeiptr, const void*, GLenum);
typedef void   (APIENTRYP PFNGLENABLEVERTEXATTRIBARRAYPROC)(GLuint);
typedef void   (APIENTRYP PFNGLVERTEXATTRIBPOINTERPROC)(GLuint, GLint, GLenum, GLboolean, GLsizei, const void*);
typedef void   (APIENTRYP PFNGLDELETEPROGRAMPROC)(GLuint);
typedef void   (APIENTRYP PFNGLDELETESHADERPROC)(GLuint);
typedef void   (APIENTRYP PFNGLGENQUERIESPROC)(GLsizei, GLuint*);
typedef void   (APIENTRYP PFNGLBEGINQUERYPROC)(GLenum, GLuint);
typedef void   (APIENTRYP PFNGLENDQUERYPROC)(GLenum);
typedef void   (APIENTRYP PFNGLGETQUERYOBJECTUI64VPROC)(GLuint, GLenum, GLuint64*);
// VAO
typedef void (APIENTRYP PFNGLGENVERTEXARRAYSPROC)(GLsizei, GLuint*);
typedef void (APIENTRYP PFNGLBINDVERTEXARRAYPROC)(GLuint);

static PFNGLCREATEPROGRAMPROC           pglCreateProgram;
static PFNGLCREATESHADERPROC            pglCreateShader;
static PFNGLSHADERSOURCEPROC            pglShaderSource;
static PFNGLCOMPILESHADERPROC           pglCompileShader;
static PFNGLGETSHADERIVPROC             pglGetShaderiv;
static PFNGLGETSHADERINFOLOGPROC        pglGetShaderInfoLog;
static PFNGLATTACHSHADERPROC            pglAttachShader;
static PFNGLLINKPROGRAMPROC             pglLinkProgram;
static PFNGLGETPROGRAMIVPROC            pglGetProgramiv;
static PFNGLGETPROGRAMINFOLOGPROC       pglGetProgramInfoLog;
static PFNGLUSEPROGRAMPROC              pglUseProgram;
static PFNGLGETUNIFORMLOCATIONPROC      pglGetUniformLocation;
static PFNGLUNIFORM1FPROC               pglUniform1f;
static PFNGLUNIFORM1IPROC               pglUniform1i;
static PFNGLUNIFORM3FPROC               pglUniform3f;
static PFNGLUNIFORM4FPROC               pglUniform4f;
static PFNGLGENBUFFERSPROC              pglGenBuffers;
static PFNGLBINDBUFFERPROC              pglBindBuffer;
static PFNGLBUFFERDATAPROC              pglBufferData;
static PFNGLENABLEVERTEXATTRIBARRAYPROC pglEnableVertexAttribArray;
static PFNGLVERTEXATTRIBPOINTERPROC     pglVertexAttribPointer;
static PFNGLDELETEPROGRAMPROC           pglDeleteProgram;
static PFNGLDELETESHADERPROC            pglDeleteShader;
static PFNGLGENQUERIESPROC              pglGenQueries;
static PFNGLBEGINQUERYPROC              pglBeginQuery;
static PFNGLENDQUERYPROC                pglEndQuery;
static PFNGLGETQUERYOBJECTUI64VPROC     pglGetQueryObjectui64v;
static PFNGLGENVERTEXARRAYSPROC         pglGenVertexArrays;
static PFNGLBINDVERTEXARRAYPROC         pglBindVertexArray;

static void* GetGL(const char* name) {
    void* p = (void*)wglGetProcAddress(name);
    if (!p) {
        HMODULE h = GetModuleHandleW(L"opengl32.dll");
        p = (void*)GetProcAddress(h, name);
    }
    return p;
}
static bool LoadGL() {
    auto need = [&](auto& f, const char* n) {
        using F = typename std::remove_reference<decltype(f)>::type;
        f = reinterpret_cast<F>(GetGL(n));
        return f != nullptr;
        };
    bool ok = true;
    ok &= need(pglCreateProgram, "glCreateProgram");
    ok &= need(pglCreateShader, "glCreateShader");
    ok &= need(pglShaderSource, "glShaderSource");
    ok &= need(pglCompileShader, "glCompileShader");
    ok &= need(pglGetShaderiv, "glGetShaderiv");
    ok &= need(pglGetShaderInfoLog, "glGetShaderInfoLog");
    ok &= need(pglAttachShader, "glAttachShader");
    ok &= need(pglLinkProgram, "glLinkProgram");
    ok &= need(pglGetProgramiv, "glGetProgramiv");
    ok &= need(pglGetProgramInfoLog, "glGetProgramInfoLog");
    ok &= need(pglUseProgram, "glUseProgram");
    ok &= need(pglGetUniformLocation, "glGetUniformLocation");
    ok &= need(pglUniform1f, "glUniform1f");
    ok &= need(pglUniform1i, "glUniform1i");
    ok &= need(pglUniform3f, "glUniform3f");
    ok &= need(pglUniform4f, "glUniform4f");
    ok &= need(pglGenBuffers, "glGenBuffers");
    ok &= need(pglBindBuffer, "glBindBuffer");
    ok &= need(pglBufferData, "glBufferData");
    ok &= need(pglEnableVertexAttribArray, "glEnableVertexAttribArray");
    ok &= need(pglVertexAttribPointer, "glVertexAttribPointer");
    ok &= need(pglDeleteProgram, "glDeleteProgram");
    ok &= need(pglDeleteShader, "glDeleteShader");
    ok &= need(pglGenQueries, "glGenQueries");
    ok &= need(pglBeginQuery, "glBeginQuery");
    ok &= need(pglEndQuery, "glEndQuery");
    ok &= need(pglGetQueryObjectui64v, "glGetQueryObjectui64v");
    ok &= need(pglGenVertexArrays, "glGenVertexArrays");
    ok &= need(pglBindVertexArray, "glBindVertexArray");
    return ok;
}

// ---------------- Globals ----------------
static HWND   g_hwnd = nullptr;
static HDC    g_hdc = nullptr;
static HGLRC  g_hglrc = nullptr;
static int    g_screenW = 0, g_screenH = 0;

static std::wstring g_fragPath;
static double g_runSeconds = 60.0;

static bool   g_started = false;
static bool   g_finishing = false;
static double g_tStart = 0.0; // first draw (sec)
static double g_tFade = 0.0;  // fade start
static const double FADE_SEC = 2.0;

// stats
static double g_scoreMult = 10.0;           // SCORE = avgMpix/s * 10
static std::vector<double>  g_fpsSamples;   // instantaneous FPS (for the window)
static std::vector<GLuint64> g_gpuNs;       // GPU ns per frame (HUD only)
static uint64_t g_sumPixels = 0;
static uint64_t g_sumNs = 0;

static int g_frame = 0;
static int g_exitScore = 0;
static int g_fpsMax = 0, g_fpsAvg10 = 0, g_fpsMin = 0;

static std::deque<double> g_frameTimes10;
static double g_fps10_display = 0.0;
static double g_lastHudUpdateSec = 0.0;
static const double HUD_UPDATE_INTERVAL = 0.1;

// ---------------- Hash ----------------
#pragma comment(lib, "bcrypt.lib")

// Pre - calculated permission hash(SHA - 256, hexadecimal string, uppercase)
static const wchar_t* kAllowedShaderSHA256[] = {
    L"7E6F5464F910D1FEB1196B6F50BFE2383761FADFC62030E5782896304D2E51E5",
    L"2237DBA4F0A602B958F144FE620708DBC3A301FB97FDD363A3F89B0718CCA89A",
    L"2F7BB2EAA770AD07D40570812FA2C6EF1867423A9E029622FE207B1998423DA2",
    L"64E0DEBE0929BCEC73C2902C851F80E2F06F62DC44EA1E7149FB5B04D447AC70",
    L"A6E8D223649641B7F4CFBE8B428A66A9FEFFED2FD9612830FC9E6095277838B6",
};

// Convert hexadecimal strings (case-insensitive) to uppercase
static std::wstring ToUpperHex(const std::wstring& s) {
    std::wstring t = s;
    std::transform(t.begin(), t.end(), t.begin(),
        [](wchar_t c) { return (wchar_t)::towupper(c); });
    return t;
}

// Byte sequence -> Hexadecimal (uppercase) string
static std::wstring BytesToHexUpper(const uint8_t* data, size_t len) {
    static const wchar_t* hex = L"0123456789ABCDEF";
    std::wstring out; out.resize(len * 2);
    for (size_t i = 0; i < len; ++i) {
        out[i * 2 + 0] = hex[(data[i] >> 4) & 0xF];
        out[i * 2 + 1] = hex[(data[i]) & 0xF];
    }
    return out;
}

// Calculate the SHA-256 hash (CNG: bcrypt) of the file (raw bytes)
static bool ComputeSHA256File(const wchar_t* path, std::array<uint8_t, 32>& out) {
    HANDLE h = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (h == INVALID_HANDLE_VALUE) return false;

    BCRYPT_ALG_HANDLE hAlg = nullptr;
    BCRYPT_HASH_HANDLE hHash = nullptr;
    NTSTATUS st = BCryptOpenAlgorithmProvider(&hAlg, BCRYPT_SHA256_ALGORITHM, nullptr, 0);
    if (st != 0) { CloseHandle(h); return false; }

    DWORD cbHashObject = 0, cbData = 0;
    st = BCryptGetProperty(hAlg, BCRYPT_OBJECT_LENGTH, (PUCHAR)&cbHashObject, sizeof(cbHashObject), &cbData, 0);
    if (st != 0) { BCryptCloseAlgorithmProvider(hAlg, 0); CloseHandle(h); return false; }

    std::vector<UCHAR> hashObject(cbHashObject);
    st = BCryptCreateHash(hAlg, &hHash, hashObject.data(), cbHashObject, nullptr, 0, 0);
    if (st != 0) { BCryptCloseAlgorithmProvider(hAlg, 0); CloseHandle(h); return false; }

    // Sequential hashing in 64KB chunks
    std::vector<BYTE> buf(64 * 1024);
    DWORD readBytes = 0;
    BOOL ok = FALSE;
    for (;;) {
        ok = ReadFile(h, buf.data(), (DWORD)buf.size(), &readBytes, nullptr);
        if (!ok) { BCryptDestroyHash(hHash); BCryptCloseAlgorithmProvider(hAlg, 0); CloseHandle(h); return false; }
        if (readBytes == 0) break; // EOF
        st = BCryptHashData(hHash, (PUCHAR)buf.data(), readBytes, 0);
        if (st != 0) { BCryptDestroyHash(hHash); BCryptCloseAlgorithmProvider(hAlg, 0); CloseHandle(h); return false; }
    }
    CloseHandle(h);

    DWORD cbHash = 32;
    st = BCryptFinishHash(hHash, (PUCHAR)out.data(), cbHash, 0);
    BCryptDestroyHash(hHash);
    BCryptCloseAlgorithmProvider(hAlg, 0);
    return (st == 0);
}

// Permission List Verification (Comparison Using Hexadecimal Strings)
static bool IsShaderHashAllowedHex(const std::wstring& hexUpper) {
    for (auto wh : kAllowedShaderSHA256) {
        if (hexUpper == wh) return true;
    }
    return false;
}

// Determines whether the file at the specified path matches the permitted hash (with exception handling using a message box)
static bool VerifyShaderFileBySHA256_Whitelist(const wchar_t* fragPath) {
    std::array<uint8_t, 32> hash{};
    if (!ComputeSHA256File(fragPath, hash)) {
        MessageBoxW(nullptr, (std::wstring(L"Hash calc failed: ") + fragPath).c_str(),
            L"Shader verify", MB_ICONERROR);
        return false;
    }
    std::wstring hex = BytesToHexUpper(hash.data(), hash.size());

    if (!IsShaderHashAllowedHex(hex)) {
        std::wstring msg = L"Shader hash not allowed.\nFile: ";
        msg += fragPath;
        msg += L"\nSHA-256: ";
        msg += hex;
        MessageBoxW(nullptr, msg.c_str(), L"Shader verify", MB_ICONERROR);
        return false;
    }
    return true;
}

// ---------------- HUD (left-top overlay) ----------------
static GLuint g_fontListBase = 0;
static int    g_fontPx = 16;
static HFONT  g_hFont = nullptr;

struct OrthoGuard {
    GLint prevProg = 0; GLboolean blend = 0; GLboolean depth = 0; GLint vp[4]{};
    OrthoGuard(int w, int h) {
        glGetIntegerv(GL_VIEWPORT, vp);
        glGetBooleanv(GL_BLEND, &blend);
        glGetBooleanv(GL_DEPTH_TEST, &depth);
        glGetIntegerv(0x8B8D/*GL_CURRENT_PROGRAM*/, &prevProg);
        if (depth) glDisable(GL_DEPTH_TEST);
        glEnable(GL_BLEND); glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        glMatrixMode(GL_PROJECTION); glPushMatrix(); glLoadIdentity(); glOrtho(0, w, h, 0, -1, 1);
        glMatrixMode(GL_MODELVIEW);  glPushMatrix(); glLoadIdentity();
        if (prevProg) pglUseProgram(0);
    }
    ~OrthoGuard() {
        glMatrixMode(GL_MODELVIEW);  glPopMatrix();
        glMatrixMode(GL_PROJECTION); glPopMatrix();
        if (!blend) glDisable(GL_BLEND);
        if (depth)  glEnable(GL_DEPTH_TEST);
        if (prevProg) pglUseProgram(prevProg);
        glViewport(vp[0], vp[1], vp[2], vp[3]);
    }
};

// ---------------- HUD (left-top overlay) ----------------

static void DestroyHudFontGL() {
    if (g_fontListBase) { glDeleteLists(g_fontListBase, 256); g_fontListBase = 0; }
    if (g_hFont) { DeleteObject(g_hFont); g_hFont = nullptr; }
}

static bool CreateHudFontGL(HDC hdc, int px) {
    DestroyHudFontGL();
    g_fontPx = px;

    g_hFont = CreateFontW(px, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE, DEFAULT_CHARSET,
        OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, ANTIALIASED_QUALITY,
        FIXED_PITCH | FF_MODERN, L"Consolas");
    if (!g_hFont) return false;

    HGDIOBJ old = SelectObject(hdc, g_hFont);
    g_fontListBase = glGenLists(256);
    if (!g_fontListBase || !wglUseFontBitmapsW(hdc, 0, 256, g_fontListBase)) {
        if (g_fontListBase) glDeleteLists(g_fontListBase, 256), g_fontListBase = 0;
        SelectObject(hdc, old);
        DestroyHudFontGL();
        return false;
    }
    SelectObject(hdc, old);
    return true;
}

// Dynamically determine font px based on DPI and window height, and rebuild if necessary.
static int ComputeHudPx(HWND hwnd, int w, int h) {
    int base = (int)std::round(h / 36.0);
    base = (std::max)(base, 12);
    base = (std::min)(base, 64);

    // DPI Compensation
    UINT dpi = 96;
    using GetDpiForWindow_t = UINT(WINAPI*)(HWND);
    HMODULE hUser32 = GetModuleHandleW(L"user32.dll");
    if (hUser32) {
        auto pGetDpiForWindow = (GetDpiForWindow_t)GetProcAddress(hUser32, "GetDpiForWindow");
        if (pGetDpiForWindow) dpi = pGetDpiForWindow(hwnd);
        else {
            HDC hdc = GetDC(hwnd);
            if (hdc) { dpi = (UINT)GetDeviceCaps(hdc, LOGPIXELSY); ReleaseDC(hwnd, hdc); }
        }
    }
    int px = MulDiv(base, (int)dpi, 96);
    px = px / 2;
    px = (std::max)(px, 12);
    px = (std::min)(px, 48);
    return px;
}

static void EnsureHudFontFor(HWND hwnd, HDC hdc, int w, int h) {
    int need = ComputeHudPx(hwnd, w, h);
    if (!g_fontListBase || need != g_fontPx) {
        CreateHudFontGL(hdc, need);
    }
}

static SIZE MeasureTextBlock(HDC hdc, HFONT f, const std::wstring& text) {
    SIZE out{ 0,0 }; if (!hdc) return out;
    HGDIOBJ old = f ? SelectObject(hdc, f) : nullptr;
    int maxw = 0, lines = 0;
    size_t pos = 0;
    while (pos <= text.size()) {
        size_t nl = text.find(L'\n', pos);
        std::wstring line = (nl == std::wstring::npos) ? text.substr(pos) : text.substr(pos, nl - pos);
        SIZE sz{ 0,0 }; std::wstring disp = line.empty() ? L" " : line;
        GetTextExtentPoint32W(hdc, disp.c_str(), (int)disp.size(), &sz);
        maxw = (std::max)(maxw, (int)sz.cx);
        lines++;
        if (nl == std::wstring::npos) break;
        pos = nl + 1;
    }
    out.cx = maxw; out.cy = lines * g_fontPx;
    if (old) SelectObject(hdc, old);
    return out;
}

static void DrawHudLeftTop(const std::wstring& text, int winW, int winH) {
    if (!g_fontListBase) return;
    OrthoGuard og(winW, winH);

    SIZE sz = MeasureTextBlock(g_hdc, g_hFont, text);
    int desiredBoxH = winH / 30;
    if (desiredBoxH < sz.cy + 4) desiredBoxH = sz.cy + 4;

    const int padX = winW / 200;

    const int padTotal = desiredBoxH - sz.cy;
    const int padTop = padTotal / 2;
    const int padBottom = padTotal - padTop;

    const int boxW = sz.cx + padX * 2;
    const int boxH = desiredBoxH;

	const int margin = 8;
    glColor4f(0, 0, 0, 0.5f);
    glBegin(GL_QUADS);
    glVertex2i(margin, margin);
    glVertex2i(margin + boxW, margin);
    glVertex2i(margin + boxW, margin + boxH);
    glVertex2i(margin, margin + boxH);
    glEnd();

    // Retrieve font baseline information (ascent/descent)
    TEXTMETRIC tm{};
    HGDIOBJ old = SelectObject(g_hdc, g_hFont);
    GetTextMetrics(g_hdc, &tm);
    SelectObject(g_hdc, old);
    const int lineH = tm.tmHeight;
    const int ascent = tm.tmAscent;

    glColor4f(0.85f, 0.88f, 0.92f, 1);

    int lineY = 0;
    size_t pos = 0;
    while (pos <= text.size()) {
        size_t nl = text.find(L'\n', pos);
        std::wstring line = (nl == std::wstring::npos) ? text.substr(pos)
            : text.substr(pos, nl - pos);

        glRasterPos2i(8 + padX, 8 + padTop + lineY + ascent);

        std::vector<GLubyte> bytes; bytes.reserve(line.size());
        for (wchar_t wc : line) {
            unsigned c = (unsigned)wc;
            bytes.push_back((GLubyte)((c <= 0xFF) ? c : '?'));
        }
        glListBase(g_fontListBase);
        if (!bytes.empty())
            glCallLists((GLsizei)bytes.size(), GL_UNSIGNED_BYTE, bytes.data());

        if (nl == std::wstring::npos) break;
        pos = nl + 1;
        lineY += lineH; // Adjust line spacing precisely according to tm.tmHeight
    }
}

// ---------------- Utils ----------------
static std::wstring GetExeDirW() {
    wchar_t path[MAX_PATH]; GetModuleFileNameW(nullptr, path, MAX_PATH);
    std::filesystem::path p(path); return p.remove_filename().wstring();
}
static bool ReadFileAllA(const std::wstring& wpath, std::string& out) {
    std::ifstream ifs(std::filesystem::path(wpath), std::ios::binary);
    if (!ifs) return false;
    out.assign(std::istreambuf_iterator<char>(ifs), std::istreambuf_iterator<char>());
    return true;
}
static double NowSec() {
    using clock = std::chrono::steady_clock;
    static const auto t0 = clock::now();
    auto d = clock::now() - t0;
    return std::chrono::duration<double>(d).count();
}
static double Quantile(std::vector<double> v, double q) {
    if (v.empty()) return 0.0;
    std::sort(v.begin(), v.end());
    double pos = (v.size() - 1) * q;
    size_t lo = (size_t)std::floor(pos), hi = (size_t)std::ceil(pos);
    if (lo == hi) return v[lo];
    double h = pos - lo; return v[lo] * (1 - h) + v[hi] * h;
}

// ---------------- GL context (3.3 compatibility) ----------------
static bool InitGLContext(HWND hwnd) {
    g_hdc = GetDC(hwnd);
    if (!g_hdc) return false;

    // legacy temp
    PIXELFORMATDESCRIPTOR pfd = { sizeof(pfd) };
    pfd.nVersion = 1; pfd.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER;
    pfd.iPixelType = PFD_TYPE_RGBA; pfd.cColorBits = 32; pfd.cDepthBits = 24; pfd.cStencilBits = 8;
    int pf = ChoosePixelFormat(g_hdc, &pfd);
    if (!pf || !SetPixelFormat(g_hdc, pf, &pfd)) return false;

    HGLRC temp = wglCreateContext(g_hdc);
    if (!temp || !wglMakeCurrent(g_hdc, temp)) return false;

    // WGL extensions
    pwglCreateContextAttribsARB = (PFNWGLCREATECONTEXTATTRIBSARBPROC)wglGetProcAddress("wglCreateContextAttribsARB");
    pwglChoosePixelFormatARB = (PFNWGLCHOOSEPIXELFORMATARBPROC)wglGetProcAddress("wglChoosePixelFormatARB");

    if (pwglCreateContextAttribsARB && pwglChoosePixelFormatARB) {
        int attr[] = {
            0x2001/*WGL_DRAW_TO_WINDOW_ARB*/, 1,
            0x2010/*WGL_SUPPORT_OPENGL_ARB*/, 1,
            0x2011/*WGL_DOUBLE_BUFFER_ARB*/,  1,
            0x2013/*WGL_PIXEL_TYPE_ARB*/,     0x202B/*WGL_TYPE_RGBA_ARB*/,
            0x2014/*WGL_COLOR_BITS_ARB*/,     32,
            0x2022/*WGL_DEPTH_BITS_ARB*/,     24,
            0x2023/*WGL_STENCIL_BITS_ARB*/,   8,
            0,0
        };
        int fmt = 0; UINT num = 0;
        if (pwglChoosePixelFormatARB(g_hdc, attr, nullptr, 1, &fmt, &num) && num > 0) {
            PIXELFORMATDESCRIPTOR pfd2{};
            DescribePixelFormat(g_hdc, fmt, sizeof(pfd2), &pfd2);
            SetPixelFormat(g_hdc, fmt, &pfd2);
        }

        int ctxAttr[] = {
            0x2091/*WGL_CONTEXT_MAJOR_VERSION_ARB*/, 3,
            0x2092/*WGL_CONTEXT_MINOR_VERSION_ARB*/, 3,
            0x9126/*WGL_CONTEXT_PROFILE_MASK_ARB*/,  0x00000002/*COMPATIBILITY*/,
            0,0
        };
        HGLRC modern = pwglCreateContextAttribsARB(g_hdc, 0, ctxAttr);
        if (modern) {
            wglMakeCurrent(nullptr, nullptr);
            wglDeleteContext(temp);
            g_hglrc = modern;
            if (!wglMakeCurrent(g_hdc, g_hglrc)) return false;
        }
        else {
            g_hglrc = temp;
        }
    }
    else {
        g_hglrc = temp;
    }

    if (!LoadGL()) return false;
    return true;
}
static void DestroyGL() {
    if (g_fontListBase) { glDeleteLists(g_fontListBase, 256); g_fontListBase = 0; }
    if (g_hFont) { DeleteObject(g_hFont); g_hFont = nullptr; }
    if (g_hglrc) { wglMakeCurrent(nullptr, nullptr); wglDeleteContext(g_hglrc); g_hglrc = nullptr; }
    if (g_hwnd && g_hdc) { ReleaseDC(g_hwnd, g_hdc); g_hdc = nullptr; }
}

// ---------------- Shader compilation ----------------
static GLuint BuildShader(GLenum type, const std::string& src) {
    GLuint sh = pglCreateShader(type);
    const char* s = src.c_str();
    pglShaderSource(sh, 1, &s, nullptr);
    pglCompileShader(sh);
    GLint ok = 0; pglGetShaderiv(sh, GL_COMPILE_STATUS, &ok);
    if (!ok) {
        GLint len = 0; pglGetShaderiv(sh, GL_INFO_LOG_LENGTH, &len);
        std::string log(len, '\0'); if (len > 1) pglGetShaderInfoLog(sh, len, &len, log.data());
        MessageBoxA(nullptr, log.c_str(), "Shader compile error", MB_ICONERROR);
        return NULL;
    }
    return sh;
}
static GLuint BuildProgram(const std::string& vsSrc, const std::string& fsSrc) {
    GLuint vs = BuildShader(GL_VERTEX_SHADER, vsSrc);
    if(vs == NULL){ return NULL; }
    GLuint fs = BuildShader(GL_FRAGMENT_SHADER, fsSrc);
    if (fs == NULL) { return NULL; }
    GLuint prog = pglCreateProgram();
    pglAttachShader(prog, vs); pglAttachShader(prog, fs);
    pglLinkProgram(prog);
    GLint ok = 0; pglGetProgramiv(prog, GL_LINK_STATUS, &ok);
    if (!ok) {
        GLint len = 0; pglGetProgramiv(prog, GL_INFO_LOG_LENGTH, &len);
        std::string log(len, '\0'); if (len > 1) pglGetProgramInfoLog(prog, len, &len, log.data());
        MessageBoxA(nullptr, log.c_str(), "Program link error", MB_ICONERROR);
        return NULL;
    }
    pglDeleteShader(vs); pglDeleteShader(fs);
    return prog;
}
static std::string SanitizeFrag(const std::string& raw) {
    std::stringstream in(raw); std::string line; std::string out;
    while (std::getline(in, line)) {
        std::string l = line;
        auto p = l.find_first_not_of(" \t\r");
        if (p != std::string::npos && l.compare(p, 8, "#version") == 0) continue;
        out += line; out += "\n";
    }
    return out;
}
static std::string MakeVS() {
    return
        "#version 330 core\n"
        "layout(location=0) in vec2 aPos;\n"
        "out vec2 vUV;\n"
        "void main(){ vUV=(aPos+1.0)*0.5; gl_Position=vec4(aPos,0.0,1.0); }\n";
}
static std::string MakeFS(const std::string& userRaw) {
    std::string body = SanitizeFrag(userRaw);
    bool hasMainImage = (body.find("void mainImage") != std::string::npos);
    bool hasMain = (body.find("void main(") != std::string::npos);

    std::ostringstream ss;
    ss
        << "#version 330 core\n"
        << "uniform vec3  iResolution;\n"
        << "uniform float iTime;\n"
        << "uniform int   iFrame;\n"
        << "uniform vec4  iMouse;\n"
        << "uniform vec4  iDate;\n"
        << "uniform sampler2D iChannel0;\n"
        << "uniform sampler2D iChannel1;\n"
        << "uniform sampler2D iChannel2;\n"
        << "uniform sampler2D iChannel3;\n"
        << "in vec2 vUV;\n"
        << "layout(location=0) out vec4 FragColor;\n"
        << "#define gl_FragColor FragColor\n";

    ss << "\n// ---- USER BEGIN ----\n" << body << "\n// ---- USER END ----\n";
    if (hasMainImage) {
        ss << "void main(){ vec2 fc=gl_FragCoord.xy; mainImage(FragColor, fc); }\n";
    }
    else if (!hasMain) {
        ss << "void main(){ vec2 fc=gl_FragCoord.xy; mainImage(FragColor, fc); }\n";
    }
    return ss.str();
}

// -------------- Geometry (fullscreen tri with VAO) --------------
static GLuint g_vbo = 0, g_vao = 0;
static void InitQuad() {
    const float verts[6] = { -1,-1,  3,-1,  -1,3 };
    pglGenVertexArrays(1, &g_vao);
    pglBindVertexArray(g_vao);

    pglGenBuffers(1, &g_vbo);
    pglBindBuffer(GL_ARRAY_BUFFER, g_vbo);
    pglBufferData(GL_ARRAY_BUFFER, sizeof(verts), verts, GL_STATIC_DRAW);

    pglEnableVertexAttribArray(0);
    pglVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, 0, (const void*)0);

    pglBindVertexArray(0);
}

// -------------- Memory-mapped result output --------------
static void MemoryMapWriteResult(int scoreInt, int p99, double favg, int p01) {
    wchar_t buf[512];
    _snwprintf_s(buf, _TRUNCATE,
        L"Version=0.0.3\r\n"
        L"Score=%d\r\n"
        L"Max=%d\r\n"
        L"Avg=%d\r\n"
        L"Min=%d\r\n"
        L"End=1\r\n",
        scoreInt, (int)llround(p99 * 10), (int)llround(favg * 10.0), (int)llround(p01 * 10));

    size_t bytes = (wcslen(buf) + 1) * sizeof(wchar_t);

    HANDLE hMap = CreateFileMappingW(INVALID_HANDLE_VALUE, nullptr, PAGE_READWRITE, 0, (DWORD)bytes, L"CM3D25");
    if (!hMap) return;
    void* p = MapViewOfFile(hMap, FILE_MAP_ALL_ACCESS, 0, 0, bytes);
    if (p) {
        memcpy(p, buf, bytes);
        UnmapViewOfFile(p);
    }
    CloseHandle(hMap);
}

// ---------------- Window proc ----------------
static GLuint g_prog = 0;
static GLint  uRes = -1, uTime = -1, uFrame = -1, uMouse = -1, uDate = -1;
static bool   g_haveTimerQuery = false;
static GLuint g_q = 0;

static LRESULT CALLBACK WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {
    case WM_KEYDOWN:
        if (wParam == VK_ESCAPE) { PostQuitMessage(0); }
        break;
    case WM_SIZE: {
        g_screenW = LOWORD(lParam);
        g_screenH = HIWORD(lParam);
        if (g_hwnd && g_hdc) EnsureHudFontFor(g_hwnd, g_hdc, g_screenW, g_screenH);
        break;
    }
    case WM_DESTROY:
        PostQuitMessage(0);
        break;
    default: break;
    }
    return DefWindowProcW(hWnd, msg, wParam, lParam);
}

// ---------------- Draw loop ----------------
static void DrawFrame(double tNow) {
    int W = g_screenW, H = g_screenH; if (W <= 0 || H <= 0) return;

    glViewport(0, 0, W, H);
    glDisable(GL_DEPTH_TEST);
    glClearColor(0, 0, 0, 1);
    glClear(GL_COLOR_BUFFER_BIT);

    // begin GPU timer (for HUD only)
    GLuint64 ns = 0;
    if (g_haveTimerQuery && !g_finishing) {
        pglBeginQuery(GL_TIME_ELAPSED, g_q);
    }

    pglUseProgram(g_prog);
    pglBindVertexArray(g_vao);

    // uniforms
    if (uRes < 0) uRes = pglGetUniformLocation(g_prog, "iResolution");
    if (uTime < 0)uTime = pglGetUniformLocation(g_prog, "iTime");
    if (uFrame < 0)uFrame = pglGetUniformLocation(g_prog, "iFrame");
    if (uMouse < 0)uMouse = pglGetUniformLocation(g_prog, "iMouse");
    if (uDate < 0) uDate = pglGetUniformLocation(g_prog, "iDate");
    pglUniform3f(uRes, (float)W, (float)H, 1.0f);
    pglUniform1f(uTime, (float)tNow);
    pglUniform1i(uFrame, g_frame);
    pglUniform4f(uMouse, 0, 0, 0, 0);
    {
        SYSTEMTIME st; GetLocalTime(&st);
        float secs = (float)(st.wHour * 3600 + st.wMinute * 60 + st.wSecond) + st.wMilliseconds / 1000.0f;
        pglUniform4f(uDate, (float)st.wYear, (float)st.wMonth, (float)st.wDay, secs);
    }

    glDrawArrays(GL_TRIANGLES, 0, 3);

    // end GPU timer
    if (g_haveTimerQuery && !g_finishing) {
        pglEndQuery(GL_TIME_ELAPSED);
        pglGetQueryObjectui64v(g_q, GL_QUERY_RESULT, &ns);
    }

    // fade overlay
    if (g_finishing) {
        double tf = NowSec() - g_tFade;
        double a = (std::min)(1.0, (std::max)(0.0, tf / FADE_SEC));
        OrthoGuard og(W, H);
        glColor4f(0, 0, 0, (float)a);
        glBegin(GL_QUADS);
        glVertex2i(0, 0); glVertex2i(W, 0); glVertex2i(W, H); glVertex2i(0, H);
        glEnd();
    }

    // FPS samples（only before finish）
    static double lastT = 0.0;
    if (!g_finishing) {
        if (lastT > 0) {
            double dt = tNow - lastT;
            if (dt > 0 && dt < 1.0) {
                g_fpsSamples.push_back(1.0 / dt);
                if (g_fpsSamples.size() > 20000)
                    g_fpsSamples.erase(g_fpsSamples.begin(), g_fpsSamples.begin() + (g_fpsSamples.size() - 20000));
            }
        }
        if (g_haveTimerQuery && ns > 0) {
            g_gpuNs.push_back(ns);
            g_sumNs += ns;
            g_sumPixels += (uint64_t)W * (uint64_t)H;
        }
    }
    lastT = tNow;

    // Score(display) = avgFPS*10 * progress
    double elapsed = g_started ? (NowSec() - g_tStart) : 0.0;
    double progress = (g_runSeconds > 0) ? (std::min)(1.0, elapsed / g_runSeconds) : 1.0;

    // FPS samples（only before finish）
    // static double lastT = 0.0;
    if (!g_finishing) {
        if (lastT > 0) {
            double dt = tNow - lastT;
            if (dt > 0 && dt < 1.0) {
                g_fpsSamples.push_back(1.0 / dt);
                if (g_fpsSamples.size() > 20000)
                    g_fpsSamples.erase(g_fpsSamples.begin(), g_fpsSamples.begin() + (g_fpsSamples.size() - 20000));
            }
        }
        if (g_haveTimerQuery && ns > 0) {
            g_gpuNs.push_back(ns);
            g_sumNs += ns;
            g_sumPixels += (uint64_t)W * (uint64_t)H;
        }
    }
    lastT = tNow;

    // ---- FPS(last 10) update ----
    if (!g_finishing) {
        g_frameTimes10.push_back(tNow);
        const size_t windowFrames = 10;
        if (g_frameTimes10.size() > windowFrames) {
            g_frameTimes10.pop_front();
        }
    }

    double fps10_now = 0.0;
    if (g_frameTimes10.size() >= 2) {
        double span = g_frameTimes10.back() - g_frameTimes10.front();
        if (span > 0.0) {
            fps10_now = (double)(g_frameTimes10.size() - 1) / span;
        }
    }

    if ((tNow - g_lastHudUpdateSec) >= HUD_UPDATE_INTERVAL) {
        g_fps10_display = fps10_now;
        g_lastHudUpdateSec = tNow;
    }

    // FPS stats
    auto fpsCopy = g_fpsSamples;
    double favg = 0.0;
    if (!fpsCopy.empty()) {
        for (double f : fpsCopy) favg += f;
        favg /= fpsCopy.size();
    }
    double fp99 = Quantile(fpsCopy, 0.99);
    double fp01 = Quantile(fpsCopy, 0.01);

    // HUD
    // GPU Mpix/s median (HUD)
    double mpixP50 = 0.0;
    if (!g_gpuNs.empty()) {
        std::vector<double> mp; mp.reserve(g_gpuNs.size());
        for (auto n : g_gpuNs) if (n > 0) {
            double m = ((double)W * (double)H) / ((double)n * 1e-9) / 1e6; // Mpix/s
            mp.push_back(m);
        }
        mpixP50 = Quantile(mp, 0.5);
    }

    double avgMpixWindow = (g_sumNs > 0) ? (((double)g_sumPixels / ((double)g_sumNs * 1e-9)) / 1e6) : 0.0;
    int scoreDisplay = (int)llround((avgMpixWindow * g_scoreMult) * progress);

    // HUD
#include <algorithm>
#include <cmath>

    std::wstringstream hud;
    hud << std::fixed;

    // ---- Score: 6-digit zero-padded (saturate to 0–999,999)----
    {
        long long s = static_cast<long long>(scoreDisplay);
        if (s < 0) s = 0;
        if (s > 999999) s = 999999;
        hud << L"Score: " << std::setfill(L' ') << std::setw(6) << s << L" ";
    }

    // ---- FPS: 4-digit integer + 1 decimal place (saturates to 0.0–9999.9)----
    {
        double f = std::max(0.0, g_fps10_display);
        int    ip = static_cast<int>(std::floor(f));
        int    frac = static_cast<int>(std::floor((f - ip) * 10.0 + 0.5));

        if (frac >= 10) { ip += 1; frac = 0; }

        if (ip > 9999) { ip = 9999; frac = 9; }

        hud << L"FPS: "
            << std::setfill(L' ') << std::setw(4) << ip
            << L"." << frac << L" ";
    }

    // Time: Two-digit integer plus one decimal place (saturates to 0.0–99.9)
    {
        double t = std::max(0.0, elapsed);
        int    ti = static_cast<int>(std::floor(t));
        int    tfrac = static_cast<int>(std::floor((t - ti) * 10.0 + 0.5));

        if (tfrac >= 10) { ti += 1; tfrac = 0; }
        if (ti > 99) { ti = 99;  tfrac = 9; }

        hud << L"Time: "
            << std::setfill(L' ') << std::setw(2) << ti
            << L"." << tfrac;
    }


    DrawHudLeftTop(hud.str(), W, H);

    SwapBuffers(g_hdc);

    g_frame++;

    if (!g_started) { g_started = true; g_tStart = NowSec(); }
    if (!g_finishing && g_started && (NowSec() - g_tStart) >= g_runSeconds) {
        g_finishing = true; g_tFade = NowSec();
    }
}

// ---------------- Entry ----------------
int APIENTRY wWinMain(HINSTANCE hInst, HINSTANCE, LPWSTR, int) {
    // CLI: arg1=.frag path, arg2=seconds (float)
    int argc = 0; LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    std::wstring exeDir = GetExeDirW();
    if (argc >= 2) {
        std::filesystem::path p(argv[1]);
        // if (!p.is_absolute()) p = std::filesystem::path(exeDir) / p;
        if (!p.is_absolute()) p = std::filesystem::path(exeDir) / L"..\\shader" / p;
        g_fragPath = p.wstring();
    }
    else {
        std::filesystem::path p(exeDir);
        p = p.parent_path() / L"shader" / L"LiveCodingGam0022.frag";
        g_fragPath = p.wstring();
    }

    if (!VerifyShaderFileBySHA256_Whitelist(g_fragPath.c_str())) {
        return 0;
    }

    if (argc >= 3) {
        wchar_t* endp = nullptr; double sec = wcstod(argv[2], &endp);
        if (endp != argv[2] && sec > 0.0) g_runSeconds = sec;
    }
    if (argv) LocalFree(argv);

    // Window
    g_screenW = GetSystemMetrics(SM_CXSCREEN);
    g_screenH = GetSystemMetrics(SM_CYSCREEN);
    const wchar_t* cls = L"OglBenchWnd";
    WNDCLASSW wc{}; wc.lpfnWndProc = WndProc; wc.hInstance = hInst;
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW); wc.lpszClassName = cls;
    wc.hbrBackground = (HBRUSH)GetStockObject(BLACK_BRUSH);
    RegisterClassW(&wc);
    HWND hwnd = CreateWindowExW(WS_EX_APPWINDOW, cls, L"OGL Benchmark", WS_POPUP,
        0, 0, g_screenW, g_screenH, nullptr, nullptr, hInst, nullptr);
    g_hwnd = hwnd;
    ShowWindow(hwnd, SW_SHOW);
    SetForegroundWindow(hwnd);

    if (!InitGLContext(hwnd)) {
        MessageBoxW(nullptr, L"OpenGL context init failed", L"Error", MB_ICONERROR);
        DestroyGL(); MemoryMapWriteResult(1, 0, 0.0, 0); return 1;
    }
    EnsureHudFontFor(g_hwnd, g_hdc, g_screenW, g_screenH);

    // read .frag
    std::string fragSrc;
    if (!ReadFileAllA(g_fragPath, fragSrc)) {
        MessageBoxW(nullptr, (L"Failed to read: " + g_fragPath).c_str(), L"Error", MB_ICONERROR);
        DestroyGL(); MemoryMapWriteResult(1, 0, 0.0, 0); return 1;
    }

    // program
    std::string vs = MakeVS();
    std::string fs = MakeFS(fragSrc);
    g_prog = BuildProgram(vs, fs);
    if (!g_prog) { DestroyGL(); MemoryMapWriteResult(1, 0, 0.0, 0); return 1; }

    // geometry
    InitQuad();

    // timer query availability
    g_haveTimerQuery = (pglGenQueries && pglBeginQuery && pglEndQuery && pglGetQueryObjectui64v);
    if (g_haveTimerQuery) pglGenQueries(1, &g_q);

    // loop
    MSG m;
    while (true) {
        while (PeekMessageW(&m, nullptr, 0, 0, PM_REMOVE)) {
            if (m.message == WM_QUIT) { DestroyGL(); return 0; }
            TranslateMessage(&m); DispatchMessageW(&m);
        }
        DrawFrame(NowSec());
        if (g_finishing && (NowSec() - g_tFade) >= FADE_SEC) break;
    }
//endloop:

    // Final score: Resolution-independent = Average Mpix/s × 10
    double favg = 0.0;
    if (!g_fpsSamples.empty()) { for (double f : g_fpsSamples) favg += f; favg /= g_fpsSamples.size(); }
    double p99 = Quantile(g_fpsSamples, 0.99);
    double p01 = Quantile(g_fpsSamples, 0.01);

    // Average Mpix/s (over entire measurement period)
    double avgMpixFinal = (g_sumNs > 0) ? (((double)g_sumPixels / ((double)g_sumNs * 1e-9)) / 1e6) : 0.0;
    int scoreInt = (int)llround(avgMpixFinal * g_scoreMult);
    if (scoreInt < 0) scoreInt = 0;

    g_fpsMax = (int)llround(p99);
    g_fpsAvg10 = (int)llround(favg * 10.0);
    g_fpsMin = (int)llround(p01);

    MemoryMapWriteResult(scoreInt, (int)llround(p99), favg, (int)llround(p01));

    {
        std::wstringstream ss;
        ss << L"SCORE " << scoreInt
            << L" | AVG " << std::fixed << std::setprecision(2) << avgMpixFinal << L" Mpix/s"
            << L" | FPS p99 " << (int)llround(p99)
            << L" avg " << std::fixed << std::setprecision(1) << favg
            << L" p1 " << (int)llround(p01);
        SetWindowTextW(g_hwnd, ss.str().c_str());
    }

    DestroyGL();
    return scoreInt;
}
