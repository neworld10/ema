# Handoff — SakurabaEma VRChat 변환 (인수인계)

> 작성: 2026-08-11 / 상태: **변환·셋업 완료, 업로드 전 단계**

## 1. 프로젝트 개요
- **Unity 프로젝트**: `/Users/neworld10/Downloads/ema` (Unity 2022.3.22f1, VRChat SDK3A 로컬 패키지)
- **원본 블렌드**: `/Users/neworld10/Downloads/SakurabaEma_ByPOWER_v1_0 2/VRChat_Conversion/SakurabaEma_ByPOWER_VRChat.blend`
- **Blender 변환 스크립트**: `/var/folders/wy/8yyqxk4j2nxcy_rvm_nwsydh0000gn/T/opencode/ema_inspect/fix_fbx.py`
  - 실행: `/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python fix_fbx.py` (2~3분)

## 2. 완료 내역 (검증 완료)
| 항목 | 내용 |
|---|---|
| Blink 셰이프키 | Face 메시에 `Blink` 생성 (윗눈꺼풀 88% 폐쇄, `BLINK_ADDED moved_verts: 2422`) |
| FBX 내보내기 | `path_mode="RELATIVE"` — 텍스처는 `Assets/VRChat_Conversion/Textures/`에 이름 매칭 |
| 본 정리 | 428→342 뼈 (사용 안 하는 그룹/뼈 cull) |
| 휴머노이드 체인 | 54개 필수 뼈 유지, Unity 컨벤션 이름 매핑 |
| Unity Rig | **Humanoid**, avatar 유효, 매핑 53뼈 (`Jaw<-FrHair_B01` 오염 제거됨) |
| 머티리얼 | 13개 Standard + 텍스처 전부 연결, 렌더모드 정리(오버레이 투명/헤어 Cutout) |
| 아바타 씬 | `Scenes/SakurabaEma_Avatar.unity` (디스크립터 구성 완료, 아래 참조) |
| **뼈 스키닝** | **3개 메시 모두 100% 웨이트 적용 확인** (아래 §5) |

## 3. 뼈(Skinning) 적용 확인 — §답변
Blender로 최종 FBX 재임포트 검증 결과 **모든 메시가 완전히 스키닝**되어 있음:
```
MESHES 3
MESH SakurabaEma_Body  verts: 86670 weighted: 86670  groups: 198  armature: SakurabaEma_VRChat_Armature
MESH SakurabaEma_Face  verts:  7484 weighted:  7484  groups:  11  armature: SakurabaEma_VRChat_Armature
MESH SakurabaEma_Hair  verts:  7130 weighted:  7130  groups: 143  armature: SakurabaEma_VRChat_Armature
ARMATURE SakurabaEma_VRChat_Armature bones: 342
```
- 모든 버텍스가 0이 아닌 웨이트 보유, 3개 메시 모두 동일 아마추어에 바인딩.
- (Unity 버텍스 수 88927/8923/7892와 다른 것은 Unity가 노멀/UV 분할 후 집계하기 때문.)

## 4. 완료된 에셋 구조 (`Assets/VRChat_Conversion/`)
```
SakurabaEma_ByPOWER_VRChat.fbx   (최종, Blink 포함)
Textures/                        (PNG 11종 + meta)
Scenes/SakurabaEma_Avatar.unity  (아바타 씬)
Editor/EmaImportVerifier.cs      (Tools > Sakuraba Ema > Verify Import)
Editor/EmaSceneSetup.cs          (Tools > Sakuraba Ema > Create Avatar Scene)
SakurabaEma_StandardMaterialFixer.cs
VRChat_Humanoid_Bone_Map.json
README_VRChat.md
VALIDATION_REPORT.txt (구버전)
VALIDATION_REPORT_IMPORT.txt (최신 검증)
SCENE_SETUP_REPORT.txt (씬 구성 상세)
```
> `.fbm` 폴더는 제거됨 (RELATIVE 경로 전환). Unity는 FBX를 외부 머티리얼 없이 내장 머티리얼로 임포트(materialImportMode=None).

## 5. Avatar Descriptor 설정 (자동 완료, Scene에 저장됨)
- **LipSync**: `VisemeBlendShape`
  - viseme 배열: `vrc.v_aa, vrc.v_oh, vrc.v_ch, vrc.v_ih, vrc.v_ou, vrc.v_e`
- **Eye Look**: `enableEyeLook = true`, 뼈 방식 (leftEye=`LeftEye`, rightEye=`RightEye`)
  - Eyelid = **Blendshapes**: `eyelidsSkinnedMesh = SakurabaEma_Face`
  - blink 인덱스: blink=85, left=83(`vrc.Blink_L`), right=84(`vrc.Blink_R`), `eyelidType = Blendshapes(1)`

## 6. 검증 요약
- `VALIDATION_REPORT_IMPORT.txt`: **EMAVERIFY PASS**
  - Humanoid avatar valid, 메시 3개, Face `Blink` 존재, 텍스처 전부 매핑
- `SCENE_SETUP_REPORT.txt`: 디스크립터 설정 전체 기록
- Unity 임포트 에러 없음 (OculusSpatializer arm64 경고는 기존·무관)

## 7. 다음 에이전트 할 일 (남은 작업)
1. **VRChat SDK 검사/업로드 준비**
   - 씬 열기 → `VRChat SDK > Show Control Panel > Builder`로 아바타 검사 실행
   - 표현식 메뉴/파라미터(expression menu, parameters)는 미생성 — 선택 작업
   - `build` 시 기본 Animation Controllers(Idle/Locomotion/FX 등) 자동 생성 확인
2. **PhysBone 추가 (미완)** — 헤어/치마/가슴 등 보존된 본 체인
   - 머티리얼: Face_Eyelid/헤어 등 투명/알파 관련 렌더링 최종 확인
3. **선택 정비**
   - `ViewPosition` 미설정 (디스크립터 필드명 `ViewPosition` Vector3) — Head 위치로 세팅 권장
   - 스케일: FBX 1m 단위, `ScaleIPD` 여부 확인
4. **재실행 시 주의** — Unity 에디터가 켜져 있으면 배치 모드 불가.
   - 에디터가 열려 있는 동안 스크립트/에셋 변경은 자동 반영되지만, 스크립트 재컴파일이 즉시 안 되는 경우가 있음(에디터 포커스 필요).
   - 자동 검증 트리거: `fix_fbx.py` 재실행 후 FBX 교체 → `EmaImportVerifier.cs`가 AutoVerify로 동작 (report mtime보다 FBX가 새로우면 실행).

## 8. 주의사항 / 함정
- **Blender 버전**: 5.2.0 LTS (`/Applications/Blender.app/Contents/MacOS/Blender`)
- **텍스처 오타**: `T_SakurambaEma_Face.png`(오타) 존재 — 실제 사용은 `T_SakurabaEma_Face.png`
- **Jaw 매핑 오염**: 휴머노이드 자동 매핑이 `Jaw`에 머리카락 뼈(`FrHair_B01`)를 붙임 → 현재 명시적 humanDescription으로 제거됨. FBX 재변환 시 다시 확인 필요.
- **SDK 구조**: 이 버전의 `VRCAvatarDescriptor`는 `customEyeLook`이 아니라 `enableEyeLook` + `customEyeLookSettings`이며, blink는 문자열이 아닌 **`eyelidsBlendshapes` 인덱스 배열**로 지정. (EmaSceneSetup.cs에 reflection 기반 처리 코드 참고)
- **검증 스크립트 재사용**: Blender FBX 웨이트 검사는 `/var/folders/.../ema_inspect/check_skin.py`
- 유저는 한글로 소통함. 보고는 간결한 한국어로.
