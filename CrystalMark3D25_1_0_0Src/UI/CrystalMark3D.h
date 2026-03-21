/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

//------------------------------------------------
// Option Flags
//------------------------------------------------

// For Task Tray Icon Feature
// #define OPTION_TASK_TRAY
// For Windows Store App Feature
// #define OPTION_UWP
// For QR Code Feature
#define OPTION_QR_CODE

//------------------------------------------------
// Global Sttings
//------------------------------------------------

#define MD5_SEACRET					"CrystalMark3d2510"
#define REGISTER_URL                "https://crystalmarkdb.com/3d25/scores/create?"
#define X_POST_HASHTAGS				_T("CrystalMark3D25")
#define X_POST_URL					_T("https://crystalmark.info")

//------------------------------------------------
// Version Information
//------------------------------------------------

#ifdef UWP
#define PRODUCT_NAME				_T("CrystalMark 3D25 Pro")
#else
#define PRODUCT_NAME				_T("CrystalMark 3D25")
#endif

#define PRODUCT_FILENAME			_T("CrystalMark3D25")
#define PRODUCT_VERSION				_T("1.0.0")
#define PRODUCT_SHORT_NAME		    _T("CM3D25")

#define PRODUCT_RELEASE				_T("2025/11/20")
#define PRODUCT_COPY_YEAR			_T("2025")
#define PRODUCT_COPY_AUTHOR			_T("hiyohiyo")
#define PRODUCT_LICENSE				_T("MIT License")

#ifdef UNICODE
#ifdef SUISHO_AOI_SUPPORT
#define PRODUCT_COPYRIGHT_1         _T("© 2025 hiyohiyo")
#define PRODUCT_COPYRIGHT_2         _T("© 2025 CrystalMark Inc.")
#define PRODUCT_COPYRIGHT_3         _T("© 2023-2025 nijihashi sola")

#elif SUISHO_SHIZUKU_SUPPORT
#define PRODUCT_COPYRIGHT_1         _T("© 2025 hiyohiyo, koinec")
#define PRODUCT_COPYRIGHT_2         _T("© 2025 CrystalMark Inc.")
#define PRODUCT_COPYRIGHT_3         _T("© 2012-2025 kirino kasumu")

#else
#define PRODUCT_COPYRIGHT_1			_T("© 2025 hiyohiyo")
#define PRODUCT_COPYRIGHT_2         _T("© 2025 CrystalMark Inc.")
#define PRODUCT_COPYRIGHT_3			_T("")
#endif
#else
#ifdef SUISHO_AOI_SUPPORT
#define PRODUCT_COPYRIGHT_1         _T("(C) 2025 hiyohiyo")
#define PRODUCT_COPYRIGHT_2         _T("(C) 2025 CrystalMark Inc.")
#define PRODUCT_COPYRIGHT_3         _T("(C) 2023-2025 nijihashi sola")
#elif SUISHO_SHIZUKU_SUPPORT
#define PRODUCT_COPYRIGHT_1         _T("(C) 2025 hiyohiyo")
#define PRODUCT_COPYRIGHT_2         _T("(C) 2025 CrystalMark Inc.")
#define PRODUCT_COPYRIGHT_3         _T("(C) 2012-2025 kirino kasumu")
#else
#define PRODUCT_COPYRIGHT_1			_T("(C) 2025 hiyohiyo, koinec")
#define PRODUCT_COPYRIGHT_2         _T("(C) 2025 CrystalMark Inc.")
#define PRODUCT_COPYRIGHT_3			_T("")
#endif
#endif

//------------------------------------------------
// URL
//------------------------------------------------

#ifdef SUISHO_AOI_SUPPORT
#define URL_MAIN_JA					_T("https://crystalmark.info/ja/aoi")
#define URL_MAIN_EN					_T("https://crystalmark.info/en/aoi")
#else
#define URL_MAIN_JA					_T("https://crystalmark.info/ja/")
#define URL_MAIN_EN 				_T("https://crystalmark.info/en/")
#endif

#define URL_CRYSTALMARKDB_JA		_T("https://crystalmarkdb.com/3d25")
#define URL_CRYSTALMARKDB_EN		_T("https://crystalmarkdb.com/3d25")

#define URL_ADS_JA					_T("https://sessions-party.com/")
#define URL_ADS_EN					_T("https://sessions-party.com/")

#define	URL_VERSION_JA				_T("https://crystalmark.info/ja/software/crystalmark3d25/crystalmark3d25-history/")
#define	URL_VERSION_EN				_T("https://crystalmark.info/en/software/crystalmark3d25/crystalmark3d25-history/")
#define	URL_LICENSE_JA				_T("https://crystalmark.info/ja/software/crystalmark3d25/crystalmark3d25-license/")
#define	URL_LICENSE_EN				_T("https://crystalmark.info/en/software/crystalmark3d25/crystalmark3d25-license/")

#define URL_HELP_JA					_T("https://crystalmark.info/ja/software/crystalmark3d25/")
#define URL_HELP_EN 				_T("https://crystalmark.info/en/software/crystalmark3d25/")

#ifdef CRYSTALMARK_3D
#define	URL_PROJECT_SITE_1		    _T("https://www.shadertoy.com/view/cltcWM")
#define URL_PROJECT_SITE_2		    _T("https://www.shadertoy.com/view/dldyz2")
#define URL_PROJECT_SITE_3	        _T("https://www.shadertoy.com/view/4cGcDm")
#define URL_PROJECT_SITE_4			_T("https://www.shadertoy.com/view/lfKyWD")
#define URL_PROJECT_SITE_5			_T("https://sessions-party.com/")
#define	URL_PROJECT_OWNER_1		    _T("https://scrapbox.io/RENARD/")
#define URL_PROJECT_OWNER_2		    _T("https://gam0022.net/")
#define URL_PROJECT_OWNER_3	        _T("https://kamoshika-vrc.github.io/")
#define URL_PROJECT_OWNER_4			_T("https://x.com/vrc_yue")
#define URL_PROJECT_OWNER_5			_T("https://x.com/FL1NE")
#elif SUISHO_AOI_SUPPORT
#define	URL_PROJECT_SITE_1		    _T("https://twitter.com/sola_no_crayon")
#define URL_PROJECT_SITE_2		    _T("https://twitter.com/harakeiko0718")
#define URL_PROJECT_SITE_3	        _T("https://instagram.com/kotomi_wicke?igshid=OGQ5ZDc2ODk2ZA==")
#define URL_PROJECT_SITE_4			_T("https://twitter.com/bellche")
#define URL_PROJECT_SITE_5			_T("")
#elif SUISHO_SHIZUKU_SUPPORT
#define	URL_PROJECT_SITE_1		    _T("https://twitter.com/kirinokasumu")
#define URL_PROJECT_SITE_2		    _T("https://linux-ha.osdn.jp/wp/")
#define URL_PROJECT_SITE_3	        _T("https://ch.nicovideo.jp/oss")
#define URL_PROJECT_SITE_4			_T("https://twitter.com/bellche")
#define URL_PROJECT_SITE_5			_T("https://suishoshizuku.com/")
#endif

//------------------------------------------------
// Cert
//------------------------------------------------

#define CERTNAME					_T("CrystalMark Inc.")

static const int RE_EXEC = 5963;

#pragma warning(disable : 4996)
