using System.Collections.Generic;

public class ShopItem
{
    public string id;
    public string type;
    public string name;
    public int price;
    public int strength;
    public int mood;
}

public class CatalogItem
{
    public string id;
    public string name;
    public int price;

    public CatalogItem(string id, string name, int price)
    {
        this.id = id;
        this.name = name;
        this.price = price;
    }
}

public class DiamondPack
{
    public int diamonds;
    public int price;

    public DiamondPack(int diamonds, int price)
    {
        this.diamonds = diamonds;
        this.price = price;
    }
}

public static class ContentDb
{
    public static readonly string[] ItemTypes =
    {
        "glasses", "cap", "watch", "belt", "dress", "scarf", "sweets", "drinks"
    };

    public static readonly CatalogItem[] Characters =
    {
        new CatalogItem("dragon", "Little Dragon", 0),
        new CatalogItem("minion", "Minion", 5000)
    };

    public static readonly CatalogItem[] Backgrounds =
    {
        new CatalogItem("classroom", "Classroom", 0),
        new CatalogItem("park", "Park", 5000),
        new CatalogItem("night", "Night", 10000)
    };

    public static readonly DiamondPack[] DiamondPacks =
    {
        new DiamondPack(50, 1000),
        new DiamondPack(100, 2000),
        new DiamondPack(150, 3000),
        new DiamondPack(200, 5000),
        new DiamondPack(400, 10000),
        new DiamondPack(1000, 15000)
    };

    public static List<ShopItem> Items()
    {
        var list = new List<ShopItem>();
        for (var i = 0; i < ItemTypes.Length; i++)
        {
            var type = ItemTypes[i];
            Add(list, type, Pretty(type), 200);
            Add(list, type, Pretty(type) + " 1", 500);
            Add(list, type, Pretty(type) + " 2", 700);
            Add(list, type, "Sun " + Pretty(type).ToLower(), 1000);
        }
        return list;
    }

    static void Add(List<ShopItem> list, string type, string name, int price)
    {
        var item = new ShopItem();
        item.id = type + "-" + price;
        item.type = type;
        item.name = name;
        item.price = price;
        item.strength = 10;
        item.mood = 30;
        list.Add(item);
    }

    static string Pretty(string type)
    {
        return char.ToUpper(type[0]) + type.Substring(1);
    }
}
