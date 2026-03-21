/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

#include <windows.h>
#include <shellapi.h>
#include <wrl.h>
#include <wil/com.h>
#include <string>
#include <filesystem>
#include <fstream>
#include <sstream>
#include <iomanip>
#include <cstring>
#include <objbase.h>
#include <cwchar>
#include <cmath>

#include <bcrypt.h>
#include <vector>
#include <array>
#include <algorithm>
#include <cctype>
#include <cstdio>

#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "shell32.lib")

#include <WebView2.h>
#include <WebView2EnvironmentOptions.h>
using namespace Microsoft::WRL;

// ---------------- Globals ----------------
HWND g_hwnd = nullptr;
ComPtr<ICoreWebView2Controller> g_controller;
ComPtr<ICoreWebView2> g_webview;
ComPtr<ICoreWebView2Environment> g_env;
EventRegistrationToken g_webresToken{}, g_msgToken{}, g_titleToken{}, g_accelToken{}, g_navToken{}, g_procFailedToken{};

static std::wstring g_fragFullPath;
static double g_runSeconds = 60.0;
static double g_scoreMult = 10.0;
static bool   g_started = false;
static UINT_PTR g_timerFinish = 0;
static UINT_PTR g_timerWatchdog = 0;
static int g_exitScore = 0;
static bool g_safeRetryUsed = false;

static int g_maxFps = 0; // p99 * 10
static int g_avgFps = 0; // avg * 10
static int g_minFps = 0; // p01 * 10

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

// ---------------- utils ----------------
static std::wstring GetExeDirW() {
    wchar_t path[MAX_PATH]; GetModuleFileNameW(nullptr, path, MAX_PATH);
    std::filesystem::path p(path); return p.remove_filename().wstring();
}
static std::filesystem::path GetShaderDir() {
    std::filesystem::path exeDir = GetExeDirW();
    std::filesystem::path cand1 = exeDir.parent_path() / L"shader";
    std::filesystem::path cand2 = exeDir / L"shader";
    std::filesystem::path cand3 = exeDir.parent_path().parent_path() / L"shader";
    auto isDir = [](const std::filesystem::path& p)->bool {
        std::error_code ec; return std::filesystem::is_directory(p, ec);
        };
    if (isDir(cand1)) return cand1;
    if (isDir(cand2)) return cand2;
    if (isDir(cand3)) return cand3;
    return cand1;
}
static bool ReadFileAll(const std::wstring& path, std::string& out) {
    std::ifstream ifs(std::filesystem::path(path), std::ios::binary);
    if (!ifs) return false;
    out.assign(std::istreambuf_iterator<char>(ifs), std::istreambuf_iterator<char>());
    return true;
}
static void SetTitleHR(const wchar_t* phase, HRESULT hr) {
    std::wstringstream ss; ss << L"[GLSL Preview] " << phase << L" hr=0x" << std::hex << (UINT)hr;
    ::SetWindowTextW(g_hwnd, ss.str().c_str());
}

// ---------------- bench.html (split ASCII) ----------------
static const char* kHtmlA = R"HA(<!doctype html><html><head><meta charset="utf-8">
<title>GLSL Preview (Timed+Fade)</title>
<link rel="icon" href="data:,">
<style>
html,body{margin:0;height:100%;background:#000;color:#ddd;font:20px system-ui}
#hud{position:fixed;left:8px;top:8px;background-color:rgba(0,0,0,0.5);padding:6px 8px;border-radius:6px;white-space:pre;user-select:none;max-width:96vw;}
canvas{display:block;width:100vw;height:100vh;background:#000}
</style></head><body>
<canvas id="cv"></canvas><div id="hud">Loading...</div>
<script>
(async function(){
  const cv=document.getElementById('cv'), hud=document.getElementById('hud');
  function post(msg){ try{ window.chrome?.webview?.postMessage(msg); }catch(e){} }
  function showErr(e){ const t=String(e&&e.message||e); hud.textContent='Error: '+t; console.error(t); }
  function logBuildError(stage, info, src){
    const head='['+stage+'] '+(info||'(no info log)');
    const numbered=(src||'').split('\n').map((ln,i)=>String(i+1).padStart(4,' ')+'| '+ln).join('\n');
    const text='Shader/Error\n'+head+'\n----- source (numbered) -----\n'+numbered;
    hud.textContent=text; console.error(text);
    document.title='GLSL Preview | '+head.replace(/\s+/g,' ').slice(0,120);
  }

  // Host will set; defaults
  let RUN_SECONDS = 60.0;
  let SCORE_MULT  = 10.0;

  let t0 = 0;                 // first-draw timestamp
  let finishedSignal = false; // host asked to finish
  let fadeStart = 0;          // fade start timestamp
  const FADE_SEC = 2.0;
  let finalSent = false;
  const GRACE_SEC = 5.0;

  function inWarmup(){ if(!t0) return true; return ((performance.now()-t0)/1000.0) < GRACE_SEC; }

  try{
    const gl=cv.getContext('webgl2',{
      antialias:false, alpha:false, preserveDrawingBuffer:false,
      powerPreference:'default', failIfMajorPerformanceCaveat:false
    });
    if(!gl){ showErr('WebGL2 not available'); return; }

    cv.addEventListener('webglcontextlost', e=>{
      e.preventDefault();
      if(finishedSignal) return;
      hud.textContent='Context lost. Reloading...';
      setTimeout(()=>location.reload(),150);
    }, false);
    cv.addEventListener('webglcontextrestored', ()=>location.reload(), false);

    // Host -> Page
    window.chrome?.webview?.addEventListener('message', ev=>{
      try{
        const m=(typeof ev.data==='string')? JSON.parse(ev.data): ev.data||{};
        if(m.type==='setRunSeconds' && typeof m.seconds==='number' && m.seconds>0){ RUN_SECONDS = m.seconds; }
        if(m.type==='setScoreMult' && typeof m.mult==='number' && m.mult>0){ SCORE_MULT = m.mult; }
        if(m.type==='finish' && !finishedSignal){
          finishedSignal = true;
          fadeStart = performance.now(); // start fade; scoring stops here
        }
      }catch(e){}
    });

    async function fetchFrag(){
      const r=await fetch('https://app.local/user.frag',{cache:'no-store'});
      if(!r.ok) throw new Error('frag fetch failed: '+r.status);
      return r.text();
    }
    let userFrag; try{ userFrag=await fetchFrag(); }catch(e){ showErr(e); return; }

    const ext=gl.getExtension('EXT_disjoint_timer_query_webgl2');

    const VS=`#version 300 es
precision highp float; precision highp int;
layout(location=0) in vec2 aPos; out vec2 vUV;
void main(){ vUV=(aPos+1.0)*0.5; gl_Position=vec4(aPos,0.0,1.0); }`;

    function sanitizeFrag(src){
      let ss=src.replace(/^\s*#version[^\n]*$/mg,'');
      ss=ss.replace(/^\s*#extension\s+GL_OES_standard_derivatives\s*:\s*enable\s*$/mg,'');
      return ss;
    }
    function makeFS(srcRaw){
      const src=sanitizeFrag(srcRaw);
      const norm=src.replace(/\/\*[\s\S]*?\*\//g,'').replace(/\/\/.*$/gm,'');
      const hasMainImage=/\bvoid\s+mainImage\s*\(/m.test(norm);
      const hasMain=/\bvoid\s+main\s*\(/m.test(norm);
      let pre=`#version 300 es
precision highp float; precision highp int;
out vec4 fragColor; in vec2 vUV;
uniform vec3 iResolution; uniform float iTime; uniform int iFrame;
uniform vec4 iMouse; uniform vec4 iDate; uniform vec3 iChannelResolution[4];
uniform sampler2D iChannel0; uniform sampler2D iChannel1; uniform sampler2D iChannel2; uniform sampler2D iChannel3;
`;
      let body='',post='';
      if(hasMainImage){ body=src; post=`void main(){ vec2 fragCoord=gl_FragCoord.xy; mainImage(fragColor,fragCoord); }`;
      }else if(hasMain){ pre+=`#define gl_FragColor fragColor
#define main user_main_impl
`; body=src; post=`#undef main
void main(){ user_main_impl(); }`;
      }else{ body=src; post=`void main(){ vec2 fragCoord=gl_FragCoord.xy; mainImage(fragColor,fragCoord); }`; }
      return pre+"\n// ---- USER FRAG START ----\n"+body+"\n// ---- USER FRAG END ----\n"+post;
    }
)HA";

static const char* kHtmlB = R"HB(
    function sh(t,src){
      const o=gl.createShader(t); gl.shaderSource(o,src); gl.compileShader(o);
      if(!gl.getShaderParameter(o,gl.COMPILE_STATUS)){
        const info=gl.getShaderInfoLog(o)||'(no info log)';
        logBuildError(t===gl.VERTEX_SHADER?'VS':'FS',info,src);
        throw new Error('shader compile failed: '+info);
      } return o;
    }
    function mkprog(vs,fs){
      const p=gl.createProgram();
      gl.attachShader(p,vs); gl.attachShader(p,fs);
      gl.bindAttribLocation(p,0,'aPos'); gl.linkProgram(p);
      if(!gl.getProgramParameter(p,gl.LINK_STATUS)){
        const info=gl.getProgramInfoLog(p)||'(no program info log)';
        logBuildError('LINK',info,'(none)');
        throw new Error('program link failed: '+info);
      } return p;
    }
    let program=null,uRes,uTime,uFrame,uMouse,uDate,uChRes,uCh=[0,0,0,0];
    function buildProgram(){
      const vs=sh(gl.VERTEX_SHADER,VS);
      const fsSource=makeFS(userFrag);
      const fs=sh(gl.FRAGMENT_SHADER,fsSource);
      const p=mkprog(vs,fs);
      gl.deleteShader(vs); gl.deleteShader(fs);
      return p;
    }
    try{ program=buildProgram(); }catch(e){ return; }

    function grabUniforms(){
      uRes=gl.getUniformLocation(program,'iResolution');
      uTime=gl.getUniformLocation(program,'iTime');
      uFrame=gl.getUniformLocation(program,'iFrame');
      uMouse=gl.getUniformLocation(program,'iMouse');
      uDate=gl.getUniformLocation(program,'iDate');
      uChRes=gl.getUniformLocation(program,'iChannelResolution');
      uCh[0]=gl.getUniformLocation(program,'iChannel0');
      uCh[1]=gl.getUniformLocation(program,'iChannel1');
      uCh[2]=gl.getUniformLocation(program,'iChannel2');
      uCh[3]=gl.getUniformLocation(program,'iChannel3');
    }
    grabUniforms();

    const vao=gl.createVertexArray(); gl.bindVertexArray(vao);
    const vbo=gl.createBuffer(); gl.bindBuffer(gl.ARRAY_BUFFER,vbo);
    gl.bufferData(gl.ARRAY_BUFFER,new Float32Array([-1,-1,3,-1,-1,3]),gl.STATIC_DRAW);
    gl.enableVertexAttribArray(0); gl.vertexAttribPointer(0,2,gl.FLOAT,false,0,0);
    gl.bindVertexArray(null);
    gl.disable(gl.DEPTH_TEST); gl.disable(gl.SCISSOR_TEST);
    gl.clearColor(0,0,0,1);

    const MAX_DPR=2.0;
    function resizePreview(){
      const dpr=Math.max(1,Math.min(MAX_DPR,window.devicePixelRatio||1));
      const w=Math.max(1,Math.floor(innerWidth*dpr));
      const h=Math.max(1,Math.floor(innerHeight*dpr));
      if(cv.width!==w||cv.height!==h){ cv.width=w; cv.height=h; }
    }
    resizePreview(); addEventListener('resize',resizePreview);

    function make1x1(r,g,b,a){ const t=gl.createTexture(); gl.bindTexture(gl.TEXTURE_2D,t);
      gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.NEAREST);
      gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.NEAREST);
      gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);
      gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);
      const px=new Uint8Array([r,g,b,a]); gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,1,1,0,gl.RGBA,gl.UNSIGNED_BYTE,px); return t; }
    const chTex=[make1x1(0,0,0,255),make1x1(0,0,0,255),make1x1(0,0,0,255),make1x1(0,0,0,255)];
    gl.useProgram(program);
    gl.activeTexture(gl.TEXTURE0); gl.bindTexture(gl.TEXTURE_2D,chTex[0]); gl.uniform1i(uCh[0],0);
    gl.activeTexture(gl.TEXTURE1); gl.bindTexture(gl.TEXTURE_2D,chTex[1]); gl.uniform1i(uCh[1],1);
    gl.activeTexture(gl.TEXTURE2); gl.bindTexture(gl.TEXTURE_2D,chTex[2]); gl.uniform1i(uCh[2],2);
    gl.activeTexture(gl.TEXTURE3); gl.bindTexture(gl.TEXTURE_2D,chTex[3]); gl.uniform1i(uCh[3],3);
    if(uChRes){ gl.uniform3fv(uChRes,new Float32Array([1,1,1,1,1,1,1,1,1,1,1,1])); }

    const mouse=[0,0,0,0]; let down=false; let downOrigin=[0,0];
    cv.addEventListener('mousemove',e=>{
      const rect=cv.getBoundingClientRect(); const dpr=(window.devicePixelRatio||1);
      mouse[0]=(e.clientX-rect.left)*dpr; mouse[1]=(rect.height-(e.clientY-rect.top))*dpr;
      if(down){ mouse[2]=downOrigin[0]; mouse[3]=downOrigin[1]; }
    });
    cv.addEventListener('mousedown',e=>{
      const rect=cv.getBoundingClientRect(); const dpr=(window.devicePixelRatio||1);
      down=true; downOrigin=[(e.clientX-rect.left)*dpr,(rect.height-(e.clientY-rect.top))*dpr];
      mouse[2]=downOrigin[0]; mouse[3]=downOrigin[1];
    });
    addEventListener('mouseup',()=>{down=false; mouse[2]=mouse[3]=0;});

    // Fade overlay
    let fadeProg=null, uFadeAlpha=null;
    function buildFadeProg(){
      if(fadeProg) return;
      const vss = `#version 300 es
precision highp float;
layout(location=0) in vec2 aPos;
void main(){ gl_Position=vec4(aPos,0.0,1.0); }`;
      const fss = `#version 300 es
precision highp float;
out vec4 fragColor;
uniform float uAlpha;
void main(){ fragColor=vec4(0.0,0.0,0.0, uAlpha); }`;
      const v=gl.createShader(gl.VERTEX_SHADER); gl.shaderSource(v,vss); gl.compileShader(v);
      if(!gl.getShaderParameter(v,gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(v)||'fade VS error');
      const f=gl.createShader(gl.FRAGMENT_SHADER); gl.shaderSource(f,fss); gl.compileShader(f);
      if(!gl.getShaderParameter(f,gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(f)||'fade FS error');
      fadeProg=gl.createProgram(); gl.attachShader(fadeProg,v); gl.attachShader(fadeProg,f);
      gl.bindAttribLocation(fadeProg,0,'aPos'); gl.linkProgram(fadeProg);
      if(!gl.getProgramParameter(fadeProg,gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(fadeProg)||'fade LINK error');
      gl.deleteShader(v); gl.deleteShader(f);
      uFadeAlpha = gl.getUniformLocation(fadeProg,'uAlpha');
    }

    // ---- Stats ----
    const pend=[];                  // outstanding timer queries
    let gpuNsSamples=[];            // per-frame GPU ns
    let mpixSamples=[];             // per-frame Mpix/s (= (w*h)/ns)
    let sumPixels=0, sumNs=0;
    let frame=0, lastTs=0;
    let tFps0 = 0;                  // The starting point of FPS statistics (the moment the first GPU timer result is obtained)

    // ---- FPS robustness extras ----
    let dtAvg = 0;                 // Moving Average dt (ms)
    const DT_AVG_ALPHA = 0.1;      // Smoothing coefficient of the moving average
    let suspendFpsUntil = 0;       // Deadline for temporarily suspending FPS collection (ms, perf.now)
    function suspendFps(ms){ suspendFpsUntil = Math.max(suspendFpsUntil, performance.now() + ms); }
    addEventListener('visibilitychange', ()=> { suspendFps(500); });
    addEventListener('focus', ()=> { suspendFps(500); });
    addEventListener('blur',  ()=> { suspendFps(500); });
    addEventListener('resize',()=> { suspendFps(500); });
    cv.addEventListener('webglcontextrestored', ()=> { suspendFps(500); });

    // ---- FPS window(10 frames) ----
    const WIN_N = 10;
    let winDtMs = [];
    let fpsWinSamples = [];
)HB";

static const char* kHtmlC = R"HC(
    function quantile(arr,q){ if(!arr.length) return 0; const b=arr.slice().sort((a,b)=>a-b); const pos=(b.length-1)*q; const lo=Math.floor(pos), hi=Math.ceil(pos); if(lo===hi) return b[lo]; const h=pos-lo; return b[lo]*(1-h)+b[hi]*h; }
    function median(a){ return quantile(a,0.5); }
    function winsorize(x, lo, hi){ return Math.max(lo, Math.min(hi, x)); }
    function fpsFromDt(ms){ return 1000.0 / ms; } // ms→FPS

    function drawFrame(ts){
      const ext=gl.getExtension('EXT_disjoint_timer_query_webgl2');
      const dpr=Math.max(1,Math.min(2.0,window.devicePixelRatio||1));
      const targetW=Math.max(1,Math.floor(innerWidth*dpr));
      const targetH=Math.max(1,Math.floor(innerHeight*dpr));
      if(cv.width!==targetW||cv.height!==targetH){ cv.width=targetW; cv.height=targetH; }

      if(!lastTs){ lastTs=ts; requestAnimationFrame(drawFrame); return; }
      const dt=ts-lastTs; lastTs=ts;

      if (dt > 0) {
        dtAvg = dtAvg ? (dtAvg*(1.0-DT_AVG_ALPHA) + dt*DT_AVG_ALPHA) : dt;
      }

      try{
        gl.bindFramebuffer(gl.FRAMEBUFFER,null);
        gl.viewport(0,0,cv.width,cv.height);
        gl.clear(gl.COLOR_BUFFER_BIT);
        gl.useProgram(program);
        gl.bindVertexArray(vao);

        const fading = finishedSignal;
        let q=null;
        if(ext && pend.length===0 && !fading){
          q=gl.createQuery(); gl.beginQuery(ext.TIME_ELAPSED_EXT,q);
        }

        const tNow = t0 ? (performance.now() - t0) / 1000.0 : 0.0;
        if(!uRes||!uTime||!uFrame||!uMouse||!uDate){
          uRes=gl.getUniformLocation(program,'iResolution');
          uTime=gl.getUniformLocation(program,'iTime');
          uFrame=gl.getUniformLocation(program,'iFrame');
          uMouse=gl.getUniformLocation(program,'iMouse');
          uDate=gl.getUniformLocation(program,'iDate');
        }
        gl.uniform3f(uRes,cv.width,cv.height,1.0);
        gl.uniform1f(uTime,tNow);
        gl.uniform1i(uFrame,frame|0);
        gl.uniform4f(uMouse,mouse[0],mouse[1],mouse[2],mouse[3]);
        { const d=new Date();
          gl.uniform4f(uDate,d.getFullYear(),d.getMonth()+1,d.getDate(),
            d.getHours()*3600+d.getMinutes()*60+d.getSeconds()+d.getMilliseconds()/1000); }
        gl.drawArrays(gl.TRIANGLES,0,3);

        if(q){ gl.endQuery(ext.TIME_ELAPSED_EXT); pend.push(q); }

        if(!fading && !inWarmup()){
          const err=gl.getError();
          if(err!==gl.NO_ERROR && (frame%60===0)) console.warn('glError',err);
          if(ext && gl.getParameter(ext.GPU_DISJOINT_EXT) && (frame%120===0)) console.warn('GPU disjoint');
        }

        if(fading){
          const t = (performance.now() - fadeStart)/1000.0;
          const a = Math.max(0.0, Math.min(1.0, t/FADE_SEC));
          buildFadeProg();
          gl.enable(gl.BLEND);
          gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
          gl.useProgram(fadeProg);
          gl.bindVertexArray(vao);
          gl.uniform1f(uFadeAlpha, a);
          gl.drawArrays(gl.TRIANGLES,0,3);
          gl.disable(gl.BLEND);
          if(t >= FADE_SEC && !finalSent){ finalSent = true; sendFinalAndStop(); }
        }

      }catch(e){
        console.error('draw exception',e);
      }

      if(!t0){
        t0 = performance.now();
        lastTs = t0;            // dt for the next frame relative to t0
        post({type:'started'});
      }

      // Sample Collection (GPU Timer)
      if(!finishedSignal && ext){
        for(let i=pend.length-1;i>=0;i--){
          const q=pend[i];
          const avail=gl.getQueryParameter(q, gl.QUERY_RESULT_AVAILABLE);
          const dis=gl.getParameter(ext.GPU_DISJOINT_EXT);
          if(avail){
            if(!dis){
              const ns=gl.getQueryParameter(q, gl.QUERY_RESULT); // nanoseconds
              gpuNsSamples.push(ns);
              const mpix = (cv.width*cv.height)/(ns*1e-9)/1e6;
              if(isFinite(mpix) && mpix>0){ mpixSamples.push(mpix); }
              sumPixels += (cv.width * cv.height);
              sumNs     += ns;

              // Set the baseline for FPS measurement upon completion of the initial stable frame
              if(!tFps0){
                tFps0 = performance.now();
                lastTs = tFps0;
                winDtMs = [];
                fpsWinSamples = [];
              }
            }
            gl.deleteQuery(q); pend.splice(i,1);
          }
        }
      }

      let fpsWin = 0.0;
      // FPS Collection (10-frame window) - Only after tFps0, outside warmup, and after resuming
      if(!finishedSignal && tFps0 && !inWarmup()){
        const now = performance.now();
        if (now >= suspendFpsUntil && dt > 0) {
          // Burst Guard: Ignores extremely lightweight frames immediately after an ultra-heavy state
          if (!(dtAvg > 80 && dt < 6)) {
            const dtClamped = Math.max(2.5, Math.min(10000.0, dt)); // 2.5ms-10s
            winDtMs.push(dtClamped);
            if (winDtMs.length > WIN_N) winDtMs.shift();

            // Calculate FPS from the total time of the most recent WIN_N frames (N/total seconds)
            let sumMs = 0;
            for (let i=0;i<winDtMs.length;i++) sumMs += winDtMs[i];
            const frames = winDtMs.length;
            if (sumMs > 0 && frames > 0) {
              fpsWin = frames / (sumMs / 1000.0);
              const fpsClamped = fpsWin; // winsorize(fpsWin, 0.01, 240.0);
              fpsWinSamples.push(fpsClamped);
            //  if (fpsWinSamples.length > 20000) fpsWinSamples.splice(0, fpsWinSamples.length - 20000);
            }
          }
        }
      }

      // ---- HUD & Title ----
      const elapsed = t0? ((performance.now()-t0)/1000.0) : 0;
      const safeRun = Math.max(0.001, RUN_SECONDS);
      const progress = Math.max(0, Math.min(1, elapsed / safeRun));

      // Base Value: p50 Mpix/s during the period
      const p50Mpix = mpixSamples.length ? median(mpixSamples) : 0.0;

      // Display Score (Increases almost monotonically with progress)
      const scoreDisplay = p50Mpix * SCORE_MULT * progress;

      // ---- FPS (10-frame window) Statistics ----
      let p99HiFps = 0.0, p01LoFps = 0.0, avgFpsCalc = 0.0;
      if (fpsWinSamples.length > 0) {
        p99HiFps   = quantile(fpsWinSamples, 0.99);
        p01LoFps   = quantile(fpsWinSamples, 0.01);
        let sum=0; for (let i=0;i<fpsWinSamples.length;i++) sum += fpsWinSamples[i];
        avgFpsCalc = sum / fpsWinSamples.length;
      }

      const med_ns = gpuNsSamples.length? median(gpuNsSamples) : 0;
      const med_ms = med_ns/1e6;
      const remain  = Math.max(0, RUN_SECONDS - elapsed);

/*
      hud.textContent =
        'Time  '+elapsed.toFixed(1)+' / '+RUN_SECONDS.toFixed(2)+' s (rem '+remain.toFixed(1)+' s)\n'+
        'Preview: '+cv.width+'x'+cv.height+'  frame '+frame+'\n'+
        'FPS  p99 '+(p99HiFps?p99HiFps.toFixed(2):"--")+'  avg '+(avgFpsCalc?avgFpsCalc.toFixed(2):"--")+
        '  p1 '+(p01LoFps?p01LoFps.toFixed(2):"--")+"\n"+
        'GPU med '+(med_ms?med_ms.toFixed(3):"--")+' ms\n'+
        'Mpix/s p50 '+(p50Mpix?p50Mpix.toFixed(2):"--")+"\n"+
        'SCORE(display) '+(scoreDisplay>0?Math.round(scoreDisplay):0);

                      + ' FPS: p99 ' + (p99HiFps?p99HiFps.toFixed(2):"--")
                      +     ' avg ' + (avgFpsCalc?avgFpsCalc.toFixed(2):"--")
                      +     ' p1 '  + (p01LoFps?p01LoFps.toFixed(2):"--")
*/
      hud.textContent =  'Score: '+ (scoreDisplay>0?Math.round(scoreDisplay):0) + ' FPS: ' + (fpsWin?fpsWin.toFixed(1):"--") + ' Time: ' + elapsed.toFixed(1) + "\n"
/*                    + 'FPS: '  + (fpsWin?fpsWin.toFixed(1):"--")
                      + ' (p99 ' + (p99HiFps?p99HiFps.toFixed(1):"--")
                      + ' avg ' + (avgFpsCalc?avgFpsCalc.toFixed(1):"--")
                      + ' p1 '  + (p01LoFps?p01LoFps.toFixed(1):"--") + ')';
*/

// Six-digit zero-padding (integers only). Negative values and non-integers are treated as zero. Expanded display occurs during overflow.
function formatScore(n) {
  n = Number.isFinite(n) ? Math.max(0, Math.floor(n)) : 0;
  const s = String(n);
  return s.length < 6 ? s.padStart(6, ' ') : s;
}

// Fixed-point zero-padding formatter
function formatFixed(n, intDigits, fracDigits, placeholder = '-') {
  if (!Number.isFinite(n)) {
    return placeholder.repeat(intDigits) + (fracDigits ? '.' + placeholder.repeat(fracDigits) : '');
  }
  n = Math.max(0, n);
  const s = n.toFixed(fracDigits);
  let [i, f = ''] = s.split('.');
  if (i.length < intDigits) i = i.padStart(intDigits, ' ');
  return fracDigits > 0 ? `${i}.${f}` : i;
}

hud.textContent =
  'Score: ' + formatScore(scoreDisplay) +
  ' FPS: '   + formatFixed(fpsWin, 4, 1) +
  ' Time: '  + formatFixed(elapsed, 2, 1) + "\n";

      document.title='GLSL Preview | '+elapsed.toFixed(1)+'s / '+RUN_SECONDS.toFixed(2)+
        ' | SCORE '+(scoreDisplay>0?Math.round(scoreDisplay):0);

      frame++;
      requestAnimationFrame(drawFrame);
    }
    requestAnimationFrame(drawFrame);

    function sendFinalAndStop(){
      const p50MpixFinal = mpixSamples.length ? median(mpixSamples) : 0.0;
      const score = p50MpixFinal * SCORE_MULT;
      const scoreInt = Math.max(0, Math.min(0x7fffffff, Math.round(score)));

      let p99Hi=0, p01Lo=0, avgFps=0;
      if (fpsWinSamples.length > 0) {
        p99Hi = quantile(fpsWinSamples, 0.99);
        p01Lo = quantile(fpsWinSamples, 0.01);
        let sum=0; for (let i=0;i<fpsWinSamples.length;i++) sum += fpsWinSamples[i];
        avgFps = sum / fpsWinSamples.length;
      }

      post({ type:'final', maxFps:p99Hi||0, avgFps:avgFps||0, minFps:p01Lo||0,
             avgMpix: p50MpixFinal, scale: 1.0, score: score, scoreInt: scoreInt });
    }

  }catch(e){ showErr(e); }
})();
</script></body></html>
)HC";

static std::string BuildHtml() {
    std::string s; s.reserve(120 * 1024);
    s.append(kHtmlA); s.append(kHtmlB); s.append(kHtmlC);
    return s;
}

// ---------------- IStream helper ----------------
static ComPtr<IStream> MakeStreamFromBytes(const void* data, size_t size) {
    HGLOBAL h = GlobalAlloc(GMEM_MOVEABLE, size);
    if (!h) return nullptr;
    void* p = GlobalLock(h);
    if (p && size) std::memcpy(p, data, size);
    if (p) GlobalUnlock(h);
    ComPtr<IStream> s;
    if (FAILED(CreateStreamOnHGlobal(h, TRUE, &s))) { GlobalFree(h); return nullptr; }
    return s;
}

// ---------------- Serve https://app.local/* ----------------
static HRESULT __stdcall OnWebResourceRequested(ICoreWebView2*, ICoreWebView2WebResourceRequestedEventArgs* args) {
    ComPtr<ICoreWebView2WebResourceRequest> req; if (FAILED(args->get_Request(&req)) || !req) return S_OK;
    wil::unique_cotaskmem_string uri; req->get_Uri(&uri);
    std::wstring u = uri.get() ? uri.get() : L"";
    if (u.rfind(L"https://app.local/", 0) != 0) return S_OK;

    std::string body; std::wstring contentType = L"text/plain; charset=utf-8";
    int status = 200; std::wstring reason = L"OK";
    if (u == L"https://app.local/bench.html") {
        body = BuildHtml(); contentType = L"text/html; charset=utf-8";
    }
    else if (u == L"https://app.local/user.frag") {
        if (!g_fragFullPath.empty() && ReadFileAll(g_fragFullPath, body)) {
            contentType = L"text/plain; charset=utf-8";
        }
        else {
            status = 404; reason = L"Not Found"; body = "// not found";
        }
    }
    else if (u == L"https://app.local/favicon.ico") {
        static const unsigned char ico[] = { 0x00,0x00,0x01,0x00,0x00 };
        body.assign(reinterpret_cast<const char*>(ico),
            reinterpret_cast<const char*>(ico) + sizeof(ico));
        contentType = L"image/x-icon";
    }
    else {
        status = 404; reason = L"Not Found"; body = "not found";
    }

    auto stream = MakeStreamFromBytes(body.data(), body.size());
    if (!stream) return S_OK;

    ComPtr<ICoreWebView2WebResourceResponse> resp;
    std::wstring headers = L"Content-Type: " + contentType + L"\r\nCache-Control: no-cache\r\n";
    if (g_env && SUCCEEDED(g_env->CreateWebResourceResponse(stream.Get(), status, reason.c_str(), headers.c_str(), &resp))) {
        args->put_Response(resp.Get());
    }
    return S_OK;
}

// ---------------- JSON helpers ----------------
static int ExtractJsonInt(const std::wstring& json, const std::wstring& key, int def = 0) {
    auto pos = json.find(L"\"" + key + L"\"");
    if (pos == std::wstring::npos) return def;
    pos = json.find(L":", pos);
    if (pos == std::wstring::npos) return def;
    pos++;
    while (pos < json.size() && iswspace(json[pos])) pos++;
    bool neg = false; if (pos < json.size() && json[pos] == L'-') { neg = true; pos++; }
    long long val = 0; bool any = false;
    while (pos < json.size() && iswdigit(json[pos])) { any = true; val = val * 10 + (json[pos] - L'0'); pos++; }
    if (!any) return def;
    if (neg) val = -val;
    if (val < 0) val = 0;
    if (val > 0x7fffffff) val = 0x7fffffff;
    return (int)val;
}
static double ExtractJsonDouble(const std::wstring& json, const std::wstring& key, double def = 0.0) {
    auto pos = json.find(L"\"" + key + L"\"");
    if (pos == std::wstring::npos) return def;
    pos = json.find(L":", pos);
    if (pos == std::wstring::npos) return def;
    pos++;
    while (pos < json.size() && iswspace(json[pos])) pos++;
    wchar_t* endp = nullptr;
    double v = wcstod(json.c_str() + pos, &endp);
    if (endp == json.c_str() + pos) return def;
    return v;
}

// ---------------- Forward decls ----------------
static void InitWebView2WithArgs(const wchar_t* addArgs);

// ---------------- WebMessage ----------------
static HRESULT __stdcall OnWebMessageReceived(ICoreWebView2*, ICoreWebView2WebMessageReceivedEventArgs* args) {
    wil::unique_cotaskmem_string json; args->get_WebMessageAsJson(&json);
    std::wstring s = json.get() ? json.get() : L"";
    if (s.find(L"\"type\":\"started\"") != std::wstring::npos) {
        if (!g_started) {
            g_started = true;
            if (g_timerFinish) KillTimer(g_hwnd, g_timerFinish);
            UINT ms = (UINT)(g_runSeconds * 1000.0);
            if (ms < 1) ms = 1;
            g_timerFinish = SetTimer(g_hwnd, 1001, ms, nullptr);
        }
    }
    else if (s.find(L"\"type\":\"final\"") != std::wstring::npos) {
        g_exitScore = ExtractJsonInt(s, L"scoreInt", 0);
        double maxFpsD = ExtractJsonDouble(s, L"maxFps", 0.0);
        double avgFpsD = ExtractJsonDouble(s, L"avgFps", 0.0);
        double minFpsD = ExtractJsonDouble(s, L"minFps", 0.0);
        g_maxFps = (int)llround(maxFpsD * 10.0);
        g_avgFps = (int)llround(avgFpsD * 10.0);
        g_minFps = (int)llround(minFpsD * 10.0);
        PostQuitMessage(0);
    }
    return S_OK;
}

// ---------------- ProcessFailed -> retry or quit ----------------
static HRESULT __stdcall OnProcessFailed(ICoreWebView2*, ICoreWebView2ProcessFailedEventArgs* args) {
    COREWEBVIEW2_PROCESS_FAILED_KIND kind{};
    args->get_ProcessFailedKind(&kind);
    if (!g_started && !g_safeRetryUsed) {
        g_safeRetryUsed = true;
        if (g_controller) g_controller->Close(); g_controller.Reset();
        g_webview.Reset(); g_env.Reset();
        InitWebView2WithArgs(L"--use-angle=d3d11 --disable-features=CalculateNativeWinOcclusion");
        return S_OK;
    }
    g_exitScore = 1; g_maxFps = g_avgFps = g_minFps = 0;
    PostQuitMessage(0);
    return S_OK;
}

// ---------------- Init WebView2 ----------------
static void WireWebViewEvents() {
    if (!g_webview || !g_controller) return;

    g_webview->add_ProcessFailed(Callback<ICoreWebView2ProcessFailedEventHandler>(OnProcessFailed).Get(), &g_procFailedToken);

    g_controller->add_AcceleratorKeyPressed(
        Callback<ICoreWebView2AcceleratorKeyPressedEventHandler>(
            [](ICoreWebView2Controller*, ICoreWebView2AcceleratorKeyPressedEventArgs* args)->HRESULT {
                COREWEBVIEW2_KEY_EVENT_KIND kind{}; UINT vk{};
                if (SUCCEEDED(args->get_KeyEventKind(&kind)) &&
                    SUCCEEDED(args->get_VirtualKey(&vk))) {
                    if (kind == COREWEBVIEW2_KEY_EVENT_KIND_KEY_DOWN && vk == VK_ESCAPE) {
                        g_exitScore = 0;
                        PostQuitMessage(0);
                        args->put_Handled(TRUE);
                    }
                }
                return S_OK;
            }).Get(),
                &g_accelToken);

    ComPtr<ICoreWebView2Settings> settings;
    if (SUCCEEDED(g_webview->get_Settings(&settings)) && settings) {
        settings->put_IsWebMessageEnabled(TRUE);
    }

    g_webview->add_NavigationCompleted(
        Callback<ICoreWebView2NavigationCompletedEventHandler>(
            [](ICoreWebView2*, ICoreWebView2NavigationCompletedEventArgs*)->HRESULT {
                std::wstringstream ss1;
                ss1 << L"{\"type\":\"setRunSeconds\",\"seconds\":" << std::fixed << std::setprecision(3) << g_runSeconds << L"}";
                g_webview->PostWebMessageAsString(ss1.str().c_str());
                std::wstringstream ss2;
                ss2 << L"{\"type\":\"setScoreMult\",\"mult\":" << std::fixed << std::setprecision(3) << g_scoreMult << L"}";
                g_webview->PostWebMessageAsString(ss2.str().c_str());
                return S_OK;
            }).Get(), &g_navToken);

    g_webview->add_DocumentTitleChanged(
        Callback<ICoreWebView2DocumentTitleChangedEventHandler>(
            [](ICoreWebView2* sender, IUnknown*)->HRESULT {
                wil::unique_cotaskmem_string t; if (SUCCEEDED(sender->get_DocumentTitle(&t)) && t) ::SetWindowTextW(g_hwnd, t.get());
                return S_OK;
            }).Get(), &g_titleToken);

    g_webview->add_WebMessageReceived(
        Callback<ICoreWebView2WebMessageReceivedEventHandler>(OnWebMessageReceived).Get(), &g_msgToken);
}

static void NavigateAppUrl() {
    if (!g_webview) return;
    g_webview->AddWebResourceRequestedFilter(L"https://app.local/*", COREWEBVIEW2_WEB_RESOURCE_CONTEXT_ALL);
    g_webview->add_WebResourceRequested(
        Callback<ICoreWebView2WebResourceRequestedEventHandler>(
            [](ICoreWebView2*, ICoreWebView2WebResourceRequestedEventArgs* args)->HRESULT {
                return OnWebResourceRequested(nullptr, args);
            }).Get(),
                &g_webresToken);
    g_webview->Navigate(L"https://app.local/bench.html");
}

static void InitWebView2WithArgs(const wchar_t* addArgs) {
	TCHAR tempPath[MAX_PATH];
	GetTempPath(MAX_PATH, tempPath);
    std::wstring dataDir = (std::filesystem::path(tempPath) / L"CrystalMark3D25WebView2").wstring();

    ComPtr<ICoreWebView2EnvironmentOptions> options;
#if __has_include(<WebView2EnvironmentOptions.h>)
    options = Make<CoreWebView2EnvironmentOptions>();
    if (options && addArgs && *addArgs) {
        options->put_AdditionalBrowserArguments(addArgs);
    }
#endif

    CreateCoreWebView2EnvironmentWithOptions(
        nullptr, dataDir.c_str(), options.Get(),
        Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>(
            [](HRESULT hrEnv, ICoreWebView2Environment* env)->HRESULT {
                if (FAILED(hrEnv)) { g_exitScore = 1; g_maxFps = g_avgFps = g_minFps = 0; PostQuitMessage(0); return hrEnv; }
                g_env = env;
                env->CreateCoreWebView2Controller(
                    g_hwnd,
                    Callback<ICoreWebView2CreateCoreWebView2ControllerCompletedHandler>(
                        [](HRESULT hrCtl, ICoreWebView2Controller* c)->HRESULT {
                            if (FAILED(hrCtl)) { g_exitScore = 1; g_maxFps = g_avgFps = g_minFps = 0; PostQuitMessage(0); return hrCtl; }
                            g_controller = c;

                            if (g_controller) {
                                ComPtr<ICoreWebView2Controller2> c2;
                                if (SUCCEEDED(g_controller->QueryInterface(IID_PPV_ARGS(&c2))) && c2) {
                                    COREWEBVIEW2_COLOR col{}; col.A = 255; col.R = 0; col.G = 0; col.B = 0;
                                    c2->put_DefaultBackgroundColor(col);
                                }
                            }

                            g_controller->put_IsVisible(TRUE);
                            RECT rc; GetClientRect(g_hwnd, &rc); g_controller->put_Bounds(rc);
                            if (FAILED(g_controller->get_CoreWebView2(&g_webview)) || !g_webview) {
                                g_exitScore = 1; g_maxFps = g_avgFps = g_minFps = 0; PostQuitMessage(0); return E_FAIL;
                            }

                            WireWebViewEvents();
                            NavigateAppUrl();
                            ::SetWindowTextW(g_hwnd, L"GLSL Preview (initializing)");
                            return S_OK;
                        }).Get());
                return S_OK;
            }).Get());
}

static void InitWebView2AndNavigate() {
    InitWebView2WithArgs(L"");
}

// ---------------- Win32 ----------------
LRESULT CALLBACK WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {
    case WM_SIZE:
        if (g_controller) { RECT r; GetClientRect(hWnd, &r); g_controller->put_Bounds(r); }
        break;
    case WM_KEYDOWN:
        if (wParam == VK_ESCAPE) { PostQuitMessage(0); }
        if (wParam == VK_F5 && g_webview) { g_webview->Reload(); }
        if (wParam == VK_F12 && g_webview) { g_webview->OpenDevToolsWindow(); }
        if ((GetKeyState(VK_CONTROL) & 0x8000) && wParam == 'R' && g_webview) {
            g_webview->PostWebMessageAsString(L"{\"type\":\"reloadFrag\"}");
        }
        break;
    case WM_TIMER:
        if (wParam == 1001) {
            if (g_webview) {
                g_webview->PostWebMessageAsString(L"{\"type\":\"finish\"}");
                if (g_timerWatchdog) KillTimer(hWnd, g_timerWatchdog);
                g_timerWatchdog = SetTimer(hWnd, 1002, 4000, nullptr);
            }
            KillTimer(hWnd, g_timerFinish); g_timerFinish = 0;
        }
        else if (wParam == 1002) {
            KillTimer(hWnd, g_timerWatchdog); g_timerWatchdog = 0;
            PostQuitMessage(0);
        }
        break;
    case WM_DESTROY: PostQuitMessage(0); break;
    }
    return DefWindowProcW(hWnd, msg, wParam, lParam);
}

// ---- Memory-mapped writer ----
void MemoryMapWriteResult()
{
    TCHAR name[32];
    TCHAR result[2048];
    HANDLE hSharedMemory = NULL;

    wsprintf(name, L"CM3D25");

    wsprintf(result,
        L"Version=0.0.1\r\n"
        L"Score=%d\r\n"
        L"Max=%d\r\n"
        L"Avg=%d\r\n"
        L"Min=%d\r\n"
        L"End=1\r\n",
        g_exitScore, g_maxFps, g_avgFps, g_minFps);

    size_t bytes = (wcslen(result) + 1) * sizeof(TCHAR);
    if (bytes == 0) return;

    hSharedMemory = CreateFileMapping(INVALID_HANDLE_VALUE, 0, PAGE_READWRITE, 0, (DWORD)bytes, name);
    if (hSharedMemory == NULL) {
        return;
    }

    TCHAR* pMemory = (TCHAR*)MapViewOfFile(hSharedMemory, FILE_MAP_ALL_ACCESS, 0, 0, bytes);
    if (pMemory != NULL)
    {
        CopyMemory(pMemory, result, bytes);
        UnmapViewOfFile(pMemory);
    }
    CloseHandle(hSharedMemory);
}

// ---- Frag path resolver ----
static std::wstring ResolveFragPathFromArg(const std::wstring& exeDir, const std::wstring& arg) {
    std::filesystem::path shaderDir = GetShaderDir();
    std::filesystem::path p(arg);
    auto existsFile = [](const std::filesystem::path& q)->bool {
        std::error_code ec; return std::filesystem::is_regular_file(q, ec);
        };
    if (p.is_absolute()) {
        if (existsFile(p)) return p.wstring();
    }
    else {
        if (p.has_parent_path()) {
            std::filesystem::path a = std::filesystem::path(exeDir) / p;
            if (existsFile(a)) return a.wstring();
            std::filesystem::path b = shaderDir / p;
            if (existsFile(b)) return b.wstring();
        }
        else {
            std::filesystem::path c = shaderDir / p;
            if (existsFile(c)) return c.wstring();
            std::filesystem::path d = std::filesystem::path(exeDir) / p;
            if (existsFile(d)) return d.wstring();
        }
    }
    return p.wstring();
}

int APIENTRY
wWinMain(_In_ HINSTANCE hInstance, _In_opt_ HINSTANCE, _In_ LPWSTR /*lpCmdLine*/, _In_ int) {
    int argc = 0; LPWSTR* argvW = CommandLineToArgvW(GetCommandLineW(), &argc);
    std::wstring exeDir = GetExeDirW();
    std::filesystem::path shaderDir = GetShaderDir();

    if (argc >= 2) {
        g_fragFullPath = ResolveFragPathFromArg(exeDir, argvW[1]);
    }
    else {
        g_fragFullPath = (shaderDir / L"LiveCodingGam0022.frag").wstring();
    }

    if (!VerifyShaderFileBySHA256_Whitelist(g_fragFullPath.c_str())) {
        return 0;
    }

    if (argc >= 3) {
        wchar_t* endp = nullptr; double sec = wcstod(argvW[2], &endp);
        if (endp != argvW[2] && sec > 0.0 && sec < 24 * 3600.0) g_runSeconds = sec;
    }
    if (argc >= 4) {
        wchar_t* endp = nullptr; double mult = wcstod(argvW[3], &endp);
        if (endp != argvW[3] && mult > 0.0 && mult < 1e6) g_scoreMult = mult;
    }
    if (argvW) LocalFree(argvW);

    const int screenW = GetSystemMetrics(SM_CXSCREEN);
    const int screenH = GetSystemMetrics(SM_CYSCREEN);
    const wchar_t* kCls = L"WebView2GLSLPreview";
    WNDCLASSEXW wc{ sizeof(wc) }; wc.lpfnWndProc = WndProc; wc.hInstance = hInstance;
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW); wc.hbrBackground = (HBRUSH)GetStockObject(BLACK_BRUSH);
    wc.lpszClassName = kCls; RegisterClassExW(&wc);

    std::wstringstream title;
    title << L"GLSL Preview - " << g_fragFullPath
        << L"  (ESC to quit, " << std::fixed << std::setprecision(2) << g_runSeconds << L"s, mult x"
        << std::fixed << std::setprecision(2) << g_scoreMult << L")";
    DWORD style = WS_POPUP;
    g_hwnd = CreateWindowExW(WS_EX_APPWINDOW, kCls, title.str().c_str(), style,
        0, 0, screenW, screenH, nullptr, nullptr, hInstance, nullptr);
    ShowWindow(g_hwnd, SW_SHOW);
    SetWindowPos(g_hwnd, HWND_TOP, 0, 0, screenW, screenH, SWP_SHOWWINDOW);

    InitWebView2AndNavigate();

    MSG m; while (GetMessageW(&m, nullptr, 0, 0)) { TranslateMessage(&m); DispatchMessageW(&m); }

    MemoryMapWriteResult();
    return g_exitScore;
}
