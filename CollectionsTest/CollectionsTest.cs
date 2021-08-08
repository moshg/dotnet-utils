using System.Linq;
using Xunit;
using mosh.Collections;

namespace CollectionsTest
{
    public class CollectionsTest
    {
        [Fact]
        public void GroupChunkByTest()
        {
            var nums = new (int, int)[] { (1, 1), (2, 1), (2, 2), (3, 1), (3, 2), (3, 3), (1, 1) };
            var chunks = new (int, int)[][]
            {
                new (int, int)[] { (1, 1) },
                new (int, int)[] { (2, 1), (2, 2) },
                new (int, int)[] { (3, 1), (3, 2), (3, 3) },
                new (int, int)[] { (1, 1) }
            };
            foreach (var (group, i) in nums.GroupChunkBy(n => n.Item1).Select((group, i) => (group, i)))
            {
                for (int k = 0; k < 3; k++)
                {
                    Assert.Equal(chunks[i][0].Item1, group.Key);
                    foreach(var (n, j) in group.Select((n, j) => (n, j)))
                    {
                        Assert.Equal(chunks[i][j], n);
                    }
                }
            }
        }
    }
}
