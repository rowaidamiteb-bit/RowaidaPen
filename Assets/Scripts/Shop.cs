using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

// المتجر: حزمة كوينز تكفي عشرين مرحلة بـ ٤ دولار (السعر نفسه ينحط بحساب قوقل بلاي وآبل بنفس رقم المنتج)
// TestMode: لين ننزل اللعبة بالمتجر، الشراء تجريبي وما ينخصم فيه شي (عشان نجربه على الجوال)
public static class Shop
{
    public const string PackId = "com.rowaida.pen.coins20";
    public static readonly bool TestMode = true;
    static StoreController store;
    static Action<bool> waiting;

    // السعر من المتجر بعملة البلد (لو ما وصل: ٤ دولار)
    public static string Price
    {
        get
        {
            Product p = store?.GetProductById(PackId);
            if (!TestMode && p != null && p.availableToPurchase && !string.IsNullOrEmpty(p.metadata.localizedPriceString))
                return p.metadata.localizedPriceString;
            return "4 دولار";
        }
    }

    public static async void Init()
    {
        if (TestMode || store != null) return;
        try
        {
            store = UnityIAPServices.StoreController();
            store.OnPurchasePending += OnPending;
            store.OnPurchaseFailed += order => { waiting?.Invoke(false); waiting = null; };
            store.OnPurchaseConfirmed += order => { };
            store.OnStoreConnected += () => store.FetchProducts(new List<ProductDefinition> { new ProductDefinition(PackId, ProductType.Consumable) });
            store.OnStoreDisconnected += description => { };
            store.OnProductsFetched += products => { };
            store.OnProductsFetchFailed += failure => { };
            await store.Connect();
        }
        catch (Exception e)
        {
            Debug.LogWarning("المتجر ما اشتغل: " + e.Message);
        }
    }

    // نشتري الحزمة، و done تقول نجح الشراء أو لا
    public static void BuyPack(Action<bool> done)
    {
        if (TestMode)
        {
            Coins.Add(Coins.Pack);
            done(true);
            return;
        }
        Product p = store?.GetProductById(PackId);
        if (p == null || !p.availableToPurchase) { done(false); return; }
        waiting = done;
        store.PurchaseProduct(PackId);
    }

    // الدفع تم (أو كان معلق من قبل وتم الحين): نعطي الكوينز ونأكد للمتجر
    static void OnPending(PendingOrder order)
    {
        Product p = order.CartOrdered.Items().FirstOrDefault()?.Product;
        if (p != null && p.definition.id == PackId) Coins.Add(Coins.Pack);
        store.ConfirmPurchase(order);
        waiting?.Invoke(true);
        waiting = null;
    }
}
