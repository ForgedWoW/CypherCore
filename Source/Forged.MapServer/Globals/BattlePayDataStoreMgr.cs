// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking.Packets.Bpay;
using Framework.Collections;
using Serilog;

namespace Forged.MapServer.Globals;

public class BattlePayDataStoreMgr : Singleton<BattlePayDataStoreMgr>
{
    public SortedDictionary<uint, BpayDisplayInfo> DisplayInfos { get; private set; } = new();
    public SortedDictionary<uint, ProductAddon> ProductAddons { get; private set; } = new();
    public List<BpayGroup> ProductGroups { get; private set; } = new();
    public SortedDictionary<uint, BpayProductInfo> ProductInfos { get; private set; } = new();
    public SortedDictionary<uint, BpayProduct> Products { get; private set; } = new();
    public List<BpayShop> ShopEntries { get; private set; } = new();
    public bool DisplayInfoExist(uint displayInfoEntry)
    {
        if (DisplayInfos.ContainsKey(displayInfoEntry))
            return true;

        Log.Logger.Information("DisplayInfoExist failed for displayInfoEntry {}", displayInfoEntry);

        return false;
    }

    public BpayDisplayInfo GetDisplayInfo(uint displayInfoEntry)
    {
        return DisplayInfos.GetValueOrDefault(displayInfoEntry);
    }

    public List<BpayProductItem> GetItemsOfProduct(uint productID)
    {
        foreach (var product in Products)
            if (product.Value.ProductId == productID)
                return product.Value.Items;

        Log.Logger.Information("GetItemsOfProduct failed for productid {}", productID);

        return null;
    }

    public BpayProduct GetProduct(uint productID)
    {
        return Products.GetValueOrDefault(productID);
    }

    // Custom properties for each product (displayinfoEntry, productInfoEntry, shopEntry are the same)
    public ProductAddon GetProductAddon(uint displayInfoEntry)
    {
        return ProductAddons.GetValueOrDefault(displayInfoEntry);
    }

    public uint GetProductGroupId(uint productId)
    {
        foreach (var shop in ShopEntries)
            if (shop.ProductID == productId)
                return shop.GroupID;

        return 0;
    }

    // This awesome function returns back the productinfo for all the two types of productid!
    public BpayProductInfo GetProductInfoForProduct(uint productID)
    {
        // Find product by subproduct id (_productInfos.productids) if not found find it by shop productid (_productInfos.productid)
        if (!ProductInfos.TryGetValue(productID, out var prod))
        {
            foreach (var productInfo in ProductInfos)
                if (productInfo.Value.ProductId == productID)
                    return productInfo.Value;

            Log.Logger.Information("GetProductInfoForProduct failed for productID {}", productID);

            return null;
        }

        return prod;
    }

    public List<BpayProduct> GetProductsOfProductInfo(uint productInfoEntry)
    {
        /*std::vector<BattlePayData::Product> subproducts = {};
      
        for (auto productInfo : _productInfos)
            if (productInfo.second.Entry == productInfoEntry)
                for (uint32 productid : productInfo.second.ProductIds)
                {
                    //TC_LOG_INFO("server.BattlePay", "GetProductsOfProductInfo: found product [{}] at productInfo [{}]", productid, productInfoEntry);
                    subproducts.push_back(*GetProduct(productid));
                }
      
        if (subproducts.size() > 0)
            return &subproducts; // warning*/

        Log.Logger.Information("GetProductsOfProductInfo failed for productInfoEntry {}", productInfoEntry);

        return null;
    }

    public void Initialize()
    {
        LoadProductAddons();
        LoadDisplayInfos();
        LoadProduct();
        LoadProductGroups();
        LoadShopEntries();
    }
    public bool ProductExist(uint productID)
    {
        if (Products.ContainsKey(productID))
            return true;

        Log.Logger.Information("ProductExist failed for productID {}", productID);

        return false;
    }
    private void LoadDisplayInfos()
    {
        Log.Logger.Information("Loading Battlepay display info ...");
        DisplayInfos.Clear();

        var result = DB.World.Query("SELECT Entry, CreatureDisplayID, VisualID, Name1, Name2, Name3, Name4, Name5, Name6, Name7, Flags, Unk1, Unk2, Unk3, UnkInt1, UnkInt2, UnkInt3 FROM battlepay_displayinfo");

        if (result == null)
            return;

        do
        {
            var fields = result.GetFields();

            var displayInfo = new BpayDisplayInfo
            {
                Entry = fields.Read<uint>(0),
                CreatureDisplayID = fields.Read<uint>(1),
                VisualID = fields.Read<uint>(2),
                Name1 = fields.Read<string>(3),
                Name2 = fields.Read<string>(4),
                Name3 = fields.Read<string>(5),
                Name4 = fields.Read<string>(6),
                Name5 = fields.Read<string>(7),
                Name6 = fields.Read<string>(8),
                Name7 = fields.Read<string>(9),
                Flags = fields.Read<uint>(10),
                Unk1 = fields.Read<uint>(11),
                Unk2 = fields.Read<uint>(12),
                Unk3 = fields.Read<uint>(13),
                UnkInt1 = fields.Read<uint>(14),
                UnkInt2 = fields.Read<uint>(15),
                UnkInt3 = fields.Read<uint>(16)
            };

            DisplayInfos.Add(fields.Read<uint>(0), displayInfo);
        } while (result.NextRow());

        result = DB.World.Query("SELECT Entry, DisplayId, VisualId, Unk, Name, DisplayInfoEntry FROM battlepay_visual");

        if (result == null)
            return;

        var visualCounter = 0;

        do
        {
            var fields = result.GetFields();

            visualCounter++;

            var visualInfo = new BpayVisual
            {
                Entry = fields.Read<uint>(0),
                DisplayId = fields.Read<uint>(1),
                VisualId = fields.Read<uint>(2),
                Unk = fields.Read<uint>(3),
                Name = fields.Read<string>(4),
                DisplayInfoEntry = fields.Read<uint>(5)
            };

            if (!DisplayInfos.TryGetValue(visualInfo.DisplayInfoEntry, out var bpayDisplayInfo))
                continue;

            bpayDisplayInfo.Visuals.Add(visualInfo);
        } while (result.NextRow());

        Log.Logger.Information(">> Loaded {} Battlepay display info with {} visual.", (ulong)DisplayInfos.Count, visualCounter);
    }

    private void LoadProduct()
    {
        Log.Logger.Information("Loading Battlepay products ...");
        Products.Clear();
        ProductInfos.Clear();

        // Product Info
        var result = DB.World.Query("SELECT Entry, ProductId, NormalPriceFixedPoint, CurrentPriceFixedPoint, ProductIds, Unk1, Unk2, UnkInts, Unk3, ChoiceType FROM battlepay_productinfo");

        if (result == null)
            return;

        do
        {
            var fields = result.GetFields();

            var productInfo = new BpayProductInfo
            {
                Entry = fields.Read<uint>(0),
                ProductId = fields.Read<uint>(1),
                NormalPriceFixedPoint = fields.Read<uint>(2),
                CurrentPriceFixedPoint = fields.Read<uint>(3)
            };

            var subproducts_stream = new StringArray(fields.Read<string>(4), ',');

            foreach (string subproduct in subproducts_stream)
                if (uint.TryParse(subproduct, out var productId))
                    productInfo.ProductIds.Add(productId); // another cool flux stuff: multiple subproducts can be added in one column

            productInfo.Unk1 = fields.Read<uint>(5);
            productInfo.Unk2 = fields.Read<uint>(6);
            productInfo.UnkInts.Add(fields.Read<uint>(7));
            productInfo.Unk3 = fields.Read<uint>(8);
            productInfo.ChoiceType = fields.Read<uint>(9);

            // we copy store the info for every product - some product info is the same for multiple products
            foreach (var subproductid in productInfo.ProductIds)
                ProductInfos.Add(subproductid, productInfo);
        } while (result.NextRow());

        // Product
        result = DB.World.Query("SELECT Entry, ProductId, Type, Flags, Unk1, DisplayId, ItemId, Unk4, Unk5, Unk6, Unk7, Unk8, Unk9, UnkString, UnkBit, UnkBits, Name FROM battlepay_product");

        if (result == null)
            return;

        do
        {
            var fields = result.GetFields();

            var product = new BpayProduct
            {
                Entry = fields.Read<uint>(0),
                ProductId = fields.Read<uint>(1),
                Type = fields.Read<byte>(2),
                Flags = fields.Read<uint>(3),
                Unk1 = fields.Read<uint>(4),
                DisplayId = fields.Read<uint>(5),
                ItemId = fields.Read<uint>(6),
                Unk4 = fields.Read<uint>(7),
                Unk5 = fields.Read<uint>(8),
                Unk6 = fields.Read<uint>(9),
                Unk7 = fields.Read<uint>(10),
                Unk8 = fields.Read<uint>(11),
                Unk9 = fields.Read<uint>(12),
                UnkString = fields.Read<string>(13),
                UnkBit = fields.Read<bool>(14),
                UnkBits = fields.Read<uint>(15),
                Name = fields.Read<string>(16) // unused in packets but useful in other ways
            };

            Products.Add(fields.Read<uint>(1), product);
        } while (result.NextRow());

        // Product Items
        result = DB.World.Query("SELECT ID, UnkByte, ItemID, Quantity, UnkInt1, UnkInt2, IsPet, PetResult, Display FROM battlepay_item");

        if (result == null)
            return;

        do
        {
            var fields = result.GetFields();

            var productID = fields.Read<uint>(1);

            if (!Products.ContainsKey(productID))
                continue;

            var productItem = new BpayProductItem
            {
                ItemID = fields.Read<uint>(2)
            };

            if (Global.ObjectMgr.GetItemTemplate(productItem.ItemID) != null)
                continue;

            productItem.Entry = fields.Read<uint>(0);
            productItem.ID = productID;
            productItem.UnkByte = fields.Read<byte>(2);
            productItem.ItemID = fields.Read<uint>(3);
            productItem.Quantity = fields.Read<uint>(4);
            productItem.UnkInt1 = fields.Read<uint>(5);
            productItem.UnkInt2 = fields.Read<uint>(6);
            productItem.IsPet = fields.Read<bool>(7);
            productItem.PetResult = fields.Read<uint>(8);
            Products[productID].Items.Add(productItem);
        } while (result.NextRow());

        Log.Logger.Information(">> Loaded {} Battlepay product infos and {} Battlepay products", (ulong)ProductInfos.Count, (ulong)Products.Count);
    }

    private void LoadProductAddons()
    {
        Log.Logger.Information("Loading Battlepay display info addons ...");
        ProductAddons.Clear();

        var result = DB.World.Query("SELECT DisplayInfoEntry, DisableListing, DisableBuy, NameColorIndex, ScriptName, Comment FROM battlepay_addon");

        if (result == null)
            return;

        do
        {
            var fields = result.GetFields();

            var productAddon = new ProductAddon
            {
                DisplayInfoEntry = fields.Read<uint>(0),
                DisableListing = fields.Read<byte>(1),
                DisableBuy = fields.Read<byte>(2),
                NameColorIndex = fields.Read<byte>(3),
                ScriptName = fields.Read<string>(4),
                Comment = fields.Read<string>(5)
            };

            ProductAddons.Add(fields.Read<uint>(0), productAddon);
        } while (result.NextRow());

        Log.Logger.Information(">> Loaded {} Battlepay product addons", (ulong)ProductAddons.Count);
    }

    private void LoadProductGroups()
    {
        Log.Logger.Information("Loading Battlepay product groups ...");
        ProductGroups.Clear();

        var result = DB.World.Query("SELECT Entry, GroupId, IconFileDataID, DisplayType, Ordering, Unk, Name, Description FROM battlepay_group");

        if (result == null)
            return;

        do
        {
            var fields = result.GetFields();

            var productGroup = new BpayGroup
            {
                Entry = fields.Read<uint>(0),
                GroupId = fields.Read<uint>(1),
                IconFileDataID = fields.Read<uint>(2),
                DisplayType = fields.Read<byte>(3),
                Ordering = fields.Read<uint>(4),
                Unk = fields.Read<uint>(5),
                Name = fields.Read<string>(6),
                Description = fields.Read<string>(7)
            };

            ProductGroups.Add(productGroup);
        } while (result.NextRow());

        Log.Logger.Information(">> Loaded {} Battlepay product groups", (ulong)ProductGroups.Count);
    }
    private void LoadShopEntries()
    {
        Log.Logger.Information("Loading Battlepay shop entries ...");
        ShopEntries.Clear();

        var result = DB.World.Query("SELECT Entry, EntryID, GroupID, ProductID, Ordering, VasServiceType, StoreDeliveryType FROM battlepay_shop");

        if (result == null)
            return;

        do
        {
            var fields = result.GetFields();

            var shopEntry = new BpayShop
            {
                Entry = fields.Read<uint>(0),
                EntryId = fields.Read<uint>(1),
                GroupID = fields.Read<uint>(2),
                ProductID = fields.Read<uint>(3),
                Ordering = fields.Read<uint>(4),
                VasServiceType = fields.Read<uint>(5),
                StoreDeliveryType = fields.Read<byte>(6)
            };

            ShopEntries.Add(shopEntry);
        } while (result.NextRow());

        Log.Logger.Information(">> Loaded {} Battlepay shop entries", (ulong)ShopEntries.Count);
    }
}