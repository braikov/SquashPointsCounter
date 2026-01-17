using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Squash.DataAccess.Entities;

namespace Squash.DataAccess.EntitiesConfiguration
{
    public class CountryConfiguration : IEntityTypeConfiguration<Country>
    {
        public void Configure(EntityTypeBuilder<Country> builder)
        {
            var seedDate = new DateTime(2000, 1, 1);

            builder.HasData(
                new Country { Id = 1, Code = "CZE", Nationality = "Чехия", CountryName = "Чехия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 2, Code = "POR", Nationality = "Португалия", CountryName = "Португалия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 3, Code = "POL", Nationality = "Полша", CountryName = "Полша", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 4, Code = "DEN", Nationality = "Дания", CountryName = "Дания", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 5, Code = "SUI", Nationality = "Швейцария", CountryName = "Швейцария", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 6, Code = "BEL", Nationality = "Белгия", CountryName = "Белгия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 7, Code = "IRL", Nationality = "Ирландия", CountryName = "Ирландия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 8, Code = "ENG", Nationality = "Англия", CountryName = "Англия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 9, Code = "ISR", Nationality = "Израел", CountryName = "Израел", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 10, Code = "ESP", Nationality = "Испания", CountryName = "Испания", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 11, Code = "AUT", Nationality = "Австрия", CountryName = "Австрия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 12, Code = "CRO", Nationality = "Хърватия", CountryName = "Хърватия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 13, Code = "HUN", Nationality = "Унгария", CountryName = "Унгария", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 14, Code = "GER", Nationality = "Германия", CountryName = "Германия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 15, Code = "SVK", Nationality = "Словакия", CountryName = "Словакия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 16, Code = "ROM", Nationality = "Румъния", CountryName = "Румъния", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 17, Code = "NED", Nationality = "Нидерландия", CountryName = "Нидерландия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 18, Code = "UKR", Nationality = "Украйна", CountryName = "Украйна", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 19, Code = "KSA", Nationality = "Саудитска Арабия", CountryName = "Саудитска Арабия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 20, Code = "NIR", Nationality = "Северна Ирландия", CountryName = "Северна Ирландия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 21, Code = "ITA", Nationality = "Италия", CountryName = "Италия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 22, Code = "JPN", Nationality = "Япония", CountryName = "Япония", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 23, Code = "FRA", Nationality = "Франция", CountryName = "Франция", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 24, Code = "MON", Nationality = "Монако", CountryName = "Монако", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 25, Code = "BRA", Nationality = "Бразилия", CountryName = "Бразилия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 26, Code = "WAL", Nationality = "Уелс", CountryName = "Уелс", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 27, Code = "MRI", Nationality = "Мавриций", CountryName = "Мавриций", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 28, Code = "BUL", Nationality = "България", CountryName = "България", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 29, Code = "EST", Nationality = "Естония", CountryName = "Естония", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 30, Code = "EGY", Nationality = "Египет", CountryName = "Египет", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 31, Code = "NOR", Nationality = "Норвегия", CountryName = "Норвегия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 32, Code = "MEX", Nationality = "Мексико", CountryName = "Мексико", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 33, Code = "PAK", Nationality = "Пакистан", CountryName = "Пакистан", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 34, Code = "CHN", Nationality = "Китай", CountryName = "Китай", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 35, Code = "MAC", Nationality = "Макао", CountryName = "Макао", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 36, Code = "LUX", Nationality = "Люксембург", CountryName = "Люксембург", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 37, Code = "GRE", Nationality = "Гърция", CountryName = "Гърция", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 38, Code = "IND", Nationality = "Индия", CountryName = "Индия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 39, Code = "RSF", Nationality = "Русия", CountryName = "Русия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 40, Code = "SCO", Nationality = "Шотландия", CountryName = "Шотландия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 41, Code = "PNG", Nationality = "Папуа Нова Гвинея", CountryName = "Папуа Нова Гвинея", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 42, Code = "USA", Nationality = "САЩ", CountryName = "САЩ", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 43, Code = "MLT", Nationality = "Малта", CountryName = "Малта", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 44, Code = "PYF", Nationality = "Френска Полинезия", CountryName = "Френска Полинезия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 45, Code = "TUR", Nationality = "Турция", CountryName = "Турция", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 46, Code = "FIN", Nationality = "Финландия", CountryName = "Финландия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 47, Code = "QAT", Nationality = "Катар", CountryName = "Катар", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 48, Code = "SWE", Nationality = "Швеция", CountryName = "Швеция", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 49, Code = "MAS", Nationality = "Малайзия", CountryName = "Малайзия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 50, Code = "KUW", Nationality = "Кувейт", CountryName = "Кувейт", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 51, Code = "CAN", Nationality = "Канада", CountryName = "Канада", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 52, Code = "THA", Nationality = "Тайланд", CountryName = "Тайланд", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 53, Code = "GUY", Nationality = "Гаяна", CountryName = "Гаяна", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 54, Code = "AUS", Nationality = "Австралия", CountryName = "Австралия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 55, Code = "ZIM", Nationality = "Зимбабве", CountryName = "Зимбабве", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 56, Code = "HKG", Nationality = "Хонконг", CountryName = "Хонконг", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 57, Code = "SIN", Nationality = "Сингапур", CountryName = "Сингапур", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 58, Code = "KOR", Nationality = "Южна Корея", CountryName = "Южна Корея", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 59, Code = "GGY", Nationality = "Гърнси", CountryName = "Гърнси", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 60, Code = "PER", Nationality = "Перу", CountryName = "Перу", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 61, Code = "IVB", Nationality = "Британски Вирджински острови", CountryName = "Британски Вирджински острови", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 62, Code = "RSA", Nationality = "Южна Африка", CountryName = "Южна Африка", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 63, Code = "TPE", Nationality = "Китайски Тайпей", CountryName = "Китайски Тайпей", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 64, Code = "MAR", Nationality = "Мароко", CountryName = "Мароко", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 65, Code = "ARG", Nationality = "Аржентина", CountryName = "Аржентина", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 66, Code = "NZL", Nationality = "Нова Зеландия", CountryName = "Нова Зеландия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 67, Code = "GBR", Nationality = "Великобритания", CountryName = "Великобритания", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 68, Code = "COL", Nationality = "Колумбия", CountryName = "Колумбия", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 69, Code = "KEN", Nationality = "Кения", CountryName = "Кения", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 70, Code = "SRI", Nationality = "Шри Ланка", CountryName = "Шри Ланка", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 },
                new Country { Id = 71, Code = "UAE", Nationality = "Обединени арабски емирства", CountryName = "Обединени арабски емирства", DateCreated = seedDate, DateUpdated = seedDate, LastOperationUserId = 0 }
            );
        }
    }
}



