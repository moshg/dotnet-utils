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
            var i = 0;
            foreach (var group in nums.GroupChunkBy(n => n.Item1))
            {
                for (int k = 0; k < 3; k++)
                {
                    Assert.Equal(chunks[i][0].Item1, group.Key);
                    var j = 0;
                    foreach(var n in group)
                    {
                        Assert.Equal(chunks[i][j], n);
                        j++;
                    }
                    Assert.Equal(chunks[i].Length, j);
                }
                i++;
            }
        }
    }
}
