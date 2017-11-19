using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Purchasing;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using System;

public enum Store
{
    GooglePlay,
    MacAppStore,
    AppleAppStore,
    WindowsStore,
    AmazonApps
}

[Serializable]
public class StoreSpecificId
{
    public string Id { get; set; }
    public Store Store { get; set; }
    public StoreSpecificId(string id, Store store)
    {
        Id = id;
        Store = store;
    }
    public StoreSpecificId(Dictionary<string, object> storeSpecificId)
    {
        var prefs = new JsonPrefs(storeSpecificId);
        Id = prefs.ValueString("Id");
        Store = (Store) Enum.Parse(typeof (Store), prefs.ValueString("Store"));
    }

    public Dictionary<string, object> Serialize()
    {
        return new Dictionary<string, object>
        {
            {"Id",Id},
            {"Store", Store.ToString()}
        };
    }
}

[Serializable]
public class XdevsProduct
{
    public string Id { get; set; }
    public ProductType Type { get; set; }
    public List<StoreSpecificId> IdOverrides { get; set; }

    public XdevsProduct(Dictionary<string, object> xdevsProduct)
    {
        var prefs = new JsonPrefs(xdevsProduct);
        Id = prefs.ValueString("Id");
        Type = (ProductType)Enum.Parse(typeof(ProductType), prefs.ValueString("Type"));
        IdOverrides = new List<StoreSpecificId>();
        List<object> idOverrides = prefs.ValueObjectList("IdOverrides");
        foreach (Dictionary<string, object> idOverride in idOverrides)
        {
            IdOverrides.Add(new StoreSpecificId(idOverride));
        }

    }
    public XdevsProduct()
    {
        IdOverrides = new List<StoreSpecificId>();
    }

    public Dictionary<string, object> Serialize()
    {
        var dict = new Dictionary<string, object>
        {
            {"Id",Id},
            {"Type", Type}
        };
        var list = new List<object>();
        if (IdOverrides != null)
            foreach (var storeSpecificId in IdOverrides)
            {
                list.Add(storeSpecificId.Serialize());
            }
        dict.Add("IdOverrides", list);
        return dict;
    }
}
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class ProductDatabase 
{
    private static ProductDatabase instance;
    private static ProductDatabase Instance
    {
        get { return instance ?? (instance = new ProductDatabase()); }
    }
    
    private List<XdevsProduct> gameProducts = new List<XdevsProduct>();
    public const string JSON_PRODUCTS_DATABASE_PATH = "Assets/Resources/products.json.txt";

    public static List<XdevsProduct> GetProducts(bool forceLoad = false)
    {
        if (forceLoad || Instance.gameProducts.Count == 0)
        {
            Instance.gameProducts.Clear();
#if UNITY_EDITOR
            using (TextReader reader = File.OpenText(ProductDatabase.JSON_PRODUCTS_DATABASE_PATH))
            {
                Deserialize(reader.ReadToEnd());
            }
#else
            var products = ((TextAsset)Resources.Load("products.json")).text;
            Deserialize(products);
#endif
        }
        return Instance.gameProducts;
    }

    public static XdevsProduct AddProduct()
    {
        var item = new XdevsProduct();
        Instance.gameProducts.Add(item);
        return item;
    }

    public static void RemoveProduct(XdevsProduct product)
    {
        Instance.gameProducts.Remove(product);
    }

    public static Dictionary<string, object> Serialize()
    {
        var dict = new Dictionary<string, object>();
        var products = new List<object>();
        foreach (var xdevsProduct in Instance.gameProducts)
        {
            products.Add(xdevsProduct.Serialize());
        }
        dict.Add("products", products);
        return dict;
    }

    private static void Deserialize(string json)
    {
        var prefs = new JsonPrefs(json);
        foreach (Dictionary<string, object> jsonProduct in prefs.ValueObjectList("products"))
        {
            Instance.gameProducts.Add(new XdevsProduct(jsonProduct));
        }
    }
}
