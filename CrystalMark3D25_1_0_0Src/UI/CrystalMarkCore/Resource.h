//{{NO_DEPENDENCIES}}
/*---------------------------------------------------------------------------*/
//       Author : hiyohiyo
//         Mail : hiyohiyo@crystalmark.info
//          Web : https://crystalmark.info/
//      License : MIT License
/*---------------------------------------------------------------------------*/

//------------------------------------------------
// Rule
//------------------------------------------------
// IDD           100 -   199
// IDR           200 -   299
// IDC Common   1000 -  1999
// IDC Product  2000 -  9999
// ID  Product 32768 - 35840
//             *WM_APP - WM_APP + 0x0BFF

//------------------------------------------------
// Rule from CommonFx.h
//------------------------------------------------
// WM_APP + 0x0000-0x0BFF: User Application
// WM_APP + 0x0C00-0x0FFF: Project Priscilla
     // WM_APP + 0x0C00-0x0CFF: Theme
     // WM_APP + 0x0D00-0x0DFF: Language
     // WP_APP + 0x0E00-0x0FFF: Reserved
// WM_APP + 0x1000-0x3FFF: User Application

//------------------------------------------------
// Dialog ID
//------------------------------------------------

#define IDD_MAIN                        101
#define IDD_FONT                        102
#define IDD_ABOUT                       103
#define IDD_QR_CODE                     104

//------------------------------------------------
// Resource ID
//------------------------------------------------

#define IDR_MENU                        110
#define IDR_ACCELERATOR                 111
#define IDR_MAINFRAME                   112
#define IDI_TRAY_ICON                   113

//------------------------------------------------
// Control ID
//------------------------------------------------

// Dialog Common
#define IDC_OK                          1001
#define IDC_CANCEL                      1002
#define IDC_SET_DEFAULT					1003

// About Dialog
#define IDC_SECRET_VOICE                1100
#define IDC_LOGO                        1101
#define IDC_PROJECT_SITE_1              1102
#define IDC_PROJECT_SITE_2              1103
#define IDC_PROJECT_SITE_3              1104
#define IDC_PROJECT_SITE_4              1105
#define IDC_PROJECT_SITE_5              1106
#define IDC_VERSION                     1107
#define IDC_RELEASE                     1108
#define IDC_COPYRIGHT1                  1109
#define IDC_COPYRIGHT2                  1110
#define IDC_COPYRIGHT3                  1111
#define IDC_LICENSE                     1112
#define IDC_EDITION                     1113
#define IDC_PROJECT_OWNER_1             1114
#define IDC_PROJECT_OWNER_2             1115
#define IDC_PROJECT_OWNER_3             1116
#define IDC_PROJECT_OWNER_4             1117
#define IDC_PROJECT_OWNER_5             1118

// Font Setting Dialog
#define IDC_FONT_FACE_COMBO		        1201
#define IDC_FONT_SCALE_COMBO			1202
#define IDC_FONT_RENDER_COMBO	        1203
#define IDC_FONT_FACE					1204
#define IDC_FONT_SCALE					1205
#define IDC_FONT_RENDER     			1206

// Main Dialog
#define IDC_CRYSTALMARK                 1300
#define IDC_SCORE                       1301
#define IDC_TWEET                       1302
#define IDC_SINGLE                      1303
#define IDC_MULTI                       1304
#define IDC_SINGLE_METER                1305
#define IDC_MULTI_METER                 1306
#define IDC_COMMENT                     1307
#define IDC_CPU_NAME                    1308
#define IDC_ARCHITECTURE                1309

/////
#define IDC_START_0                     1310
#define IDC_START_1                     1311
#define IDC_START_2                     1312
#define IDC_START_3                     1313
#define IDC_START_4                     1314
#define IDC_SCORE_0_0                   1315
#define IDC_SCORE_1_0                   1316
#define IDC_SCORE_2_0                   1317
#define IDC_SCORE_3_0                   1318
#define IDC_SCORE_4_0                   1319
#define IDC_SCORE_1_1                   1320
#define IDC_SCORE_1_2                   1321
#define IDC_SCORE_1_3                   1322
#define IDC_SCORE_1_4                   1323
#define IDC_SCORE_2_1                   1324
#define IDC_SCORE_2_2                   1325
#define IDC_SCORE_2_3                   1326
#define IDC_SCORE_2_4                   1327
#define IDC_SCORE_3_1                   1328
#define IDC_SCORE_3_2                   1329
#define IDC_SCORE_3_3                   1330
#define IDC_SCORE_3_4                   1331
#define IDC_SCORE_4_1                   1332
#define IDC_SCORE_4_2                   1333
#define IDC_SCORE_4_3                   1334
#define IDC_SCORE_4_4                   1335

#define IDC_SNS_1                       1340
#define IDC_SNS_2                       1341
#define IDC_SNS_3                       1342
#define IDC_SNS_4                       1343
#define IDC_SNS_5                       1344
#define IDC_SNS_6                       1345
#define IDC_SNS_7                       1346
#define IDC_QR                          1347

#define IDC_SUBMIT                      1348
#define IDC_SD                          1349

#define IDC_LABEL_SYSTEM_INFO_1         1350
#define IDC_LABEL_SYSTEM_INFO_2         1351
#define IDC_LABEL_SYSTEM_INFO_3         1352
#define IDC_LABEL_SYSTEM_INFO_4         1353
#define IDC_LABEL_SYSTEM_INFO_5         1354
#define IDC_LABEL_SYSTEM_INFO_6         1355

#define IDC_SYSTEM_INFO_1               1360
#define IDC_SYSTEM_INFO_2               1361
#define IDC_SYSTEM_INFO_3               1362
#define IDC_SYSTEM_INFO_4               1363
#define IDC_SYSTEM_INFO_5               1364
#define IDC_SYSTEM_INFO_6               1365

#define IDC_COMBO_GPU_INFO              1366

#define IDC_ADS_1                       1370
#define IDC_ADS_2                       1371
#define IDC_ADS_3                       1372

#define IDC_SETTINGS                    1380

#define IDC_COMMENT_UPPER               1390

/// Post Dialog
#define IDC_LABEL_NICK_NAME			    1400
#define IDC_LABEL_COMMENT			    1401
#define IDC_NICK_NAME				    1402
#define IDC_JSON						1403
#define IDC_QR_CODE						1404
#define IDC_SAVE						1405

//------------------------------------------------
// Command ID
//------------------------------------------------

// File
#define ID_EXIT                         33000
#define ID_SAVE_TEXT                    33001
#define ID_SAVE_IMAGE                   33002

// Copy
#define ID_COPY                         33100

// Theme
#define ID_ZOOM_50                      33200
#define ID_ZOOM_64                      33201
#define ID_ZOOM_75                      33202
#define ID_ZOOM_100                     33203
#define ID_ZOOM_125                     33204
#define ID_ZOOM_150                     33205
#define ID_ZOOM_200                     33206
#define ID_ZOOM_250                     33207
#define ID_ZOOM_300                     33208
#define ID_ZOOM_400                     33209
#define ID_ZOOM_500                     33210
#define ID_ZOOM_AUTO                    33211
#define ID_FONT_SETTING                 33220

// About
#define ID_ABOUT                        33300
#define ID_CRYSTALDEWWORLD              33301
#define ID_CRYSTALMARKDB                33302

// Language
#define ID_LANGUAGE_A                   33400
#define ID_LANGUAGE_O                   33401
#define ID_MAIN_UI_IN_ENGLISH           33402
#define ID_VOICE_ENGLISH                33410
#define ID_VOICE_JAPANESE               33411

// Voice Volume
#define ID_VOICE_VOLUME_000             33500
#define ID_VOICE_VOLUME_010             33501
#define ID_VOICE_VOLUME_020             33502
#define ID_VOICE_VOLUME_030             33503
#define ID_VOICE_VOLUME_040             33504
#define ID_VOICE_VOLUME_050             33505
#define ID_VOICE_VOLUME_060             33506
#define ID_VOICE_VOLUME_070             33507
#define ID_VOICE_VOLUME_080             33508
#define ID_VOICE_VOLUME_090             33509
#define ID_VOICE_VOLUME_100             33510

// Next default values for new objects
// 
#ifdef APSTUDIO_INVOKED
#ifndef APSTUDIO_READONLY_SYMBOLS
#define _APS_NEXT_RESOURCE_VALUE        200
#define _APS_NEXT_COMMAND_VALUE         33000
#define _APS_NEXT_CONTROL_VALUE         1310
#define _APS_NEXT_SYMED_VALUE           50
#endif
#endif
