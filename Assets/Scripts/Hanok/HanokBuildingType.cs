using UnityEngine;

namespace Hanok
{
    public enum BuildingType
    {
        // 주거 공간
        An_chae,         // 안채 - 여성 거주 공간, 가족 생활 공간
        Sa_rang_chae,     // 사랑채 - 남성 거주 공간, 손님 접대
        Haeng_rang_chae,  // 행랑채 - 하인들 거주, 창고
        Sa_dang_chae,     // 사당채 - 조상 제사 공간
        Byeol_dang_chae,  // 별당채 - 별도 거주 공간
        
        // 기능적 공간
        Got_gan_chae,     // 곳간채 - 창고, 저장 공간
        Jang_dok_dae,     // 장독대 - 장 저장 공간
        Dwit_gan,        // 뒷간 - 화장실

        // 조건부 공간
        Oe_yang_gan,      // 외양간 - 가축 사육 공간 (공간이 있으면)
        U_mul,           // 우물 - 물 공급 시설 (수맥을 지나고 공간이 있으면)
        An_dam,          // 안담 - 내부 담장 (주거공간이 둘 이상)
        Ba_kkat_dam,      // 바깥담 - 외부 담장
        Jwa_pan_dae,    // 좌판대 - 상품 진열대
        Jwa_sang,       // 좌상 - 식당 테이블
        
        // 조경 및 구조물
        Hwa_dan,         // 화단 - 꽃밭, 정원
        Jeon_gak,       // 정각 - 정자, 휴식 공간
        Nu_gak,          // 누각 - 높은 전망대 건물
        Ma_dang,         // 마당 - 중앙 정원 공간
    }

    static public class HanokBuildingTypes
    {
        
    }
}
