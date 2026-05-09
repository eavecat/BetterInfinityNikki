using System;
using System.Collections.Generic;
using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.GameTask.Model;
using BetterInfinityNikki.Helpers.Extensions;
using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.GameTask.Model;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.Common.Element.Assets;

public class ElementAssets : BaseAssets<ElementAssets>
{
    public RecognitionObject PromptDialogLeftBottomStar; // 弹出框左下角的星星

    public RecognitionObject BtnWhiteConfirm;
    public RecognitionObject BtnWhiteCancel;
    public RecognitionObject BtnWhiteRecover;
    public RecognitionObject BtnBlackConfirm;
    public RecognitionObject BtnBlackCancel;
    public RecognitionObject BtnBackTeyvat;
    public RecognitionObject BtnOnlineYes;
    public RecognitionObject BtnOnlineNo;
    public Lazy<RecognitionObject> BtnExitDoor;
    public RecognitionObject InDomainRo;

    public RecognitionObject PaimonMenuRo;
    public RecognitionObject InventoryRo;
    public RecognitionObject BlueTrackPoint;

    public RecognitionObject UiLeftTopCookIcon;

    public RecognitionObject SpaceKey;
    public RecognitionObject XKey;

    public RecognitionObject FriendChat;
    public RecognitionObject ChatBackButtonRo;

    public RecognitionObject PartyBtnChooseView;
    public RecognitionObject PartyBtnDelete;

    public RecognitionObject CraftCondensedResin;
    public RecognitionObject CondensedResinCount;
    public RecognitionObject fragileResinCount;
    public RecognitionObject Keyreduce;
    public RecognitionObject Keyincrease;

    public RecognitionObject BagWeaponUnchecked;
    public RecognitionObject BagWeaponChecked;
    public RecognitionObject BagArtifactUnchecked;
    public RecognitionObject BagArtifactChecked;
    public RecognitionObject BagCharacterDevelopmentItemUnchecked;
    public RecognitionObject BagCharacterDevelopmentItemChecked;
    public RecognitionObject BagFoodUnchecked;
    public RecognitionObject BagFoodChecked;
    public RecognitionObject BagMaterialUnchecked;
    public RecognitionObject BagMaterialChecked;
    public RecognitionObject BagGadgetUnchecked;
    public RecognitionObject BagGadgetChecked;
    public RecognitionObject BagQuestUnchecked;
    public RecognitionObject BagQuestChecked;
    public RecognitionObject BagPreciousItemUnchecked;
    public RecognitionObject BagPreciousItemChecked;
    public RecognitionObject BagFurnishingUnchecked;
    public RecognitionObject BagFurnishingChecked;
    public RecognitionObject BtnArtifactSalvage;
    public RecognitionObject BtnArtifactSalvageConfirm;

    public RecognitionObject BtnClaimEncounterPointsRewards;
    public RecognitionObject PrimogemRo;

    public RecognitionObject EscMailReward;
    public RecognitionObject CollectRo;

    public RecognitionObject PageCloseWhiteRo;

    public RecognitionObject SereniteaPotHomeRo;
    public RecognitionObject TeleportSereniteaPotHomeRo;
    public RecognitionObject AYuanIconRo;
    public RecognitionObject SereniteaPotLoveRo;
    public RecognitionObject SereniteaPotMoneyRo;
    public RecognitionObject SereniteapotPageClose;
    public RecognitionObject SereniteapotShopNumberBtn;

    public RecognitionObject AYuanClothRo;
    public RecognitionObject AYuanresinRo;
    public RecognitionObject SereniteapotExpBookRo;
    public RecognitionObject SereniteapotExpBookSmallRo;
    public RecognitionObject AYuanMagicmineralprecisionRo;
    public RecognitionObject AYuanMOlaRo;
    public RecognitionObject AYuanExpBottleBigRo;
    public RecognitionObject AYuanExpBottleSmallRo;
    public RecognitionObject FingerIconRo;

    public RecognitionObject LeylineDisorderIconRo;

    public RecognitionObject EscDown;
    public RecognitionObject EscWonderlandHome;
    public RecognitionObject WonderlandEnter;
    public RecognitionObject WonderlandClose;

    public RecognitionObject Index1;
    public RecognitionObject Index2;
    public RecognitionObject Index3;
    public RecognitionObject Index4;
    public List<RecognitionObject> IndexList => [Index1, Index2, Index3, Index4];
    public RecognitionObject CurrentAvatarThreshold;


#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
    private ElementAssets() : base()
    {
        Initialization(this.systemInfo);
    }

    protected ElementAssets(ISystemInfo systemInfo) : base(systemInfo)
    {
        Initialization(systemInfo);
    }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。

    private void Initialization(ISystemInfo systemInfo)
    {
        
    }
}
