# SakurabaEma VRChat 변환본

이 폴더는 `SakurabaEma_ByPOWER.pmx`를 Unity/VRChat에 가져갈 수 있도록 변환한 결과입니다.

## 결과

- FBX: `SakurabaEma_ByPOWER_VRChat.fbx`
- Blender 원본: `SakurabaEma_ByPOWER_VRChat.blend`
- 텍스처: `Textures/`
- Humanoid 본 목록: `VRChat_Humanoid_Bone_Map.json`
- Standard 셰이더 복구 도구: `SakurabaEma_StandardMaterialFixer.cs`
- Armature: `SakurabaEma_VRChat_Armature`
- Mesh: `SakurabaEma_VRChat_Mesh`
- 표준 Principled 머티리얼: 13개
- VRChat 립싱크/눈 깜빡임 별칭 셰이프키: 8개

## Unity에서 마무리

1. FBX와 `Textures` 폴더를 Unity 프로젝트의 `Assets` 아래에 복사합니다.
2. FBX Import Settings의 `Rig > Animation Type`을 `Humanoid`로 설정하고 `Configure`에서 본 매핑을 확인합니다.
3. `Materials`에서 프로젝트에 맞는 VRChat 셰이더를 지정합니다. 변환본은 MMD 전용 AlternativeFull/PostAlphaEye를 사용하지 않고 표준 Principled 기반으로 정리했습니다.
4. VRChat SDK의 Avatar Descriptor에서 LipSync를 `Viseme Blend Shapes`로 설정하고 다음 별칭을 지정합니다: `vrc.v_aa`, `vrc.v_ih`, `vrc.v_ou`, `vrc.v_e`, `vrc.v_oh`.
5. Eye Look에서 `LeftEye`와 `RightEye`를 지정합니다.
6. 머리카락/치마/가슴 움직임은 원본 MMD 물리 설정을 제거했으므로 Unity에서 VRChat PhysBone을 별도로 추가합니다.

### Standard 셰이더를 사용하는 경우

`SakurabaEma_StandardMaterialFixer.cs`를 Unity 프로젝트의 `Assets/Editor/`에 복사하고, Hierarchy에서 아바타 루트를 선택한 뒤 `Tools > Sakuraba Ema > Repair Standard Materials`를 실행합니다. 텍스처 연결과 얼굴 오버레이/헤어 알파 설정을 자동으로 정리합니다.

## 원본 기능과의 차이

- MMD 전용 셰이더(`AlternativeFull`)와 `PostAlphaEye` 화면 이펙트는 FBX 표준 머티리얼로 대체했습니다.
- PMX의 IK/강체/조인트는 VRChat에서 자동으로 동작하지 않도록 내보내지 않았습니다. 헤어/치마 본 자체는 보존되어 있습니다.
- 표정 셰이프키는 원본을 보존하고, VRChat에서 찾기 쉽도록 `vrc.*` 별칭을 추가했습니다.

## 이용규약

원본 배포 규약은 상위 폴더의 `readme_規約.txt`와 온라인 규약을 반드시 확인하세요. VRChat 업로드/재배포 가능 여부는 원작자 규약이 허용하는 범위에서만 판단해야 합니다.
