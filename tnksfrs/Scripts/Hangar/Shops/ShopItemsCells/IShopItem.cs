public interface IShopItem
{
    bool LockCondition { get; }

    bool VipCondition { get; }

    bool HideCondition { get; }

    bool ComingSoonCondition { get; }

    int Id { get; }

    int AvailabilityLevel { get; }

    string Description { get; }

    ProfileInfo.Price Price { get; }
}
