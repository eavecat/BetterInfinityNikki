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

    public RecognitionObject MeiyaliMenuRo;
    public RecognitionObject InventoryRo;
    public RecognitionObject BlueTrackPoint;

    public RecognitionObject UiLeftTopCookIcon;

    public RecognitionObject SpaceKey;
    public RecognitionObject XKey;

    public RecognitionObject FriendChat;
    public RecognitionObject ChatBackButtonRo;

    public RecognitionObject PartyBtnChooseView;
    public RecognitionObject Keyreduce;
    public RecognitionObject Keyincrease;

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
        MeiyaliMenuRo = new RecognitionObject
        {
            Name = "MeiyaliMenu",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "meiyali_menu.png", systemInfo),
            RegionOfInterest = new Rect(0, 0, CaptureRect.Width / 4, CaptureRect.Height / 4),
            DrawOnWindow = false
        }.InitTemplate();
    }
}
