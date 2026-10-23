using IndependentWork7;

namespace IndependentWork7.Tests
{
    public class CacheTests
    {
        // ---------- Конструктор ----------

        [Fact]
        public void Constructor_NewCache_IsEmpty()
        {
            var cache = new Cache<string, int>(3);

            Assert.Equal(0, cache.Count);
            Assert.Equal(3, cache.Capacity);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Constructor_NonPositiveCapacity_Throws(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Cache<string, int>(capacity));
        }

        // ---------- Add ----------

        [Fact]
        public void Add_SingleItem_ItemIsStored()
        {
            var cache = new Cache<string, int>(3);

            cache.Add("a", 1);

            Assert.True(cache.ContainsKey("a"));
            Assert.Equal(1, cache.Get("a"));
        }

        [Fact]
        public void Add_ExistingKey_UpdatesValueWithoutGrowing()
        {
            var cache = new Cache<string, int>(3);
            cache.Add("a", 1);

            cache.Add("a", 100);

            Assert.Equal(100, cache.Get("a"));
            Assert.Equal(1, cache.Count);
        }

        // ---------- Get ----------

        [Fact]
        public void Get_ExistingKeys_ReturnsCorrectValues()
        {
            var cache = new Cache<int, string>(3);
            cache.Add(1, "one");
            cache.Add(2, "two");

            Assert.Equal("one", cache.Get(1));
            Assert.Equal("two", cache.Get(2));
        }

        [Fact]
        public void Get_MissingKey_ThrowsKeyNotFoundException()
        {
            var cache = new Cache<string, int>(2);
            cache.Add("a", 1);

            Assert.Throws<KeyNotFoundException>(() => cache.Get("missing"));
        }

        [Fact]
        public void Get_EmptyCache_ThrowsKeyNotFoundException()
        {
            var cache = new Cache<string, int>(2);

            Assert.Throws<KeyNotFoundException>(() => cache.Get("a"));
        }

        // ---------- Переповнення (FIFO) ----------

        [Fact]
        public void Add_WhenFull_RemovesOldestItem()
        {
            var cache = new Cache<string, int>(3);
            cache.Add("a", 1);
            cache.Add("b", 2);
            cache.Add("c", 3);

            cache.Add("d", 4);

            Assert.False(cache.ContainsKey("a"));
            Assert.True(cache.ContainsKey("b"));
            Assert.True(cache.ContainsKey("c"));
            Assert.True(cache.ContainsKey("d"));
            Assert.Throws<KeyNotFoundException>(() => cache.Get("a"));
        }

        [Fact]
        public void Add_SeveralOverflows_EvictsInInsertionOrder()
        {
            var cache = new Cache<int, int>(2);
            cache.Add(1, 10);
            cache.Add(2, 20);
            cache.Add(3, 30); // витісняє 1
            cache.Add(4, 40); // витісняє 2

            Assert.Equal(new[] { 3, 4 }, cache.Keys);
            Assert.False(cache.ContainsKey(1));
            Assert.False(cache.ContainsKey(2));
        }

        [Fact]
        public void Get_DoesNotChangeEvictionOrder_ForFifo()
        {
            // У FIFO (на відміну від LRU) читання не "омолоджує" елемент
            var cache = new Cache<string, int>(2);
            cache.Add("a", 1);
            cache.Add("b", 2);
            cache.Get("a");

            cache.Add("c", 3);

            Assert.False(cache.ContainsKey("a"));
            Assert.True(cache.ContainsKey("b"));
        }

        [Fact]
        public void Add_CapacityOne_KeepsOnlyLastItem()
        {
            var cache = new Cache<string, int>(1);
            cache.Add("a", 1);
            cache.Add("b", 2);

            Assert.Equal(1, cache.Count);
            Assert.Equal(2, cache.Get("b"));
            Assert.False(cache.ContainsKey("a"));
        }

        // ---------- ContainsKey ----------

        [Fact]
        public void ContainsKey_ReturnsTrueForAddedAndFalseForMissing()
        {
            var cache = new Cache<string, int>(2);
            cache.Add("x", 1);

            Assert.True(cache.ContainsKey("x"));
            Assert.False(cache.ContainsKey("y"));
        }

        // ---------- Count ----------

        [Fact]
        public void Count_GrowsWithAdds()
        {
            var cache = new Cache<int, int>(5);

            cache.Add(1, 1);
            Assert.Equal(1, cache.Count);
            cache.Add(2, 2);
            Assert.Equal(2, cache.Count);
            cache.Add(3, 3);
            Assert.Equal(3, cache.Count);
        }

        [Fact]
        public void Count_NeverExceedsCapacity()
        {
            var cache = new Cache<int, int>(3);

            for (int i = 0; i < 10; i++)
            {
                cache.Add(i, i);
            }

            Assert.Equal(3, cache.Count);
            Assert.Equal(new[] { 7, 8, 9 }, cache.Keys);
        }
    }
}
