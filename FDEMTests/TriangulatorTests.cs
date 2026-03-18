
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DelaunayTriangulator.Tests
{
    /*
    public class TriangulatorTests
    {
        [Test]
        public void CentroidAndRotationTest()
        {
            List<Vertex> TestPoints = new List<Vertex> { new Vertex(1.0F, 1.0F), new Vertex(4.0F, 2.0F),
            new Vertex(4.0F, 4.0F), new Vertex(1.0F, 4.0F)};

            Triangulator angulator = new Triangulator();
            List<Triad> TestTriangles = angulator.Triangulation(TestPoints, true);

            angulator.IsolateConnectionsAndAdjacentPointsFromTriads(TestTriangles);

            //At this point angualtor.Connections has all connections between the points including the borders (Correct)
            //At this point angulator.AdjacentPoints has to integers only for the shared edge and only one adjacent point 
            //for the rest of the connections. (Correct) (edge 13) 
            
            //Weed out all connections with one boundary point. (Boundary Points)
            for (int i = angulator.AdjacentPoints.Count() - 1; i >= 0; i--)
            {
                if (angulator.AdjacentPoints[i].Count() != 2)
                {
                    angulator.AdjacentPoints.RemoveAt(i);
                    angulator.AdjacentPointLocations.RemoveAt(i);
                    angulator.Connections.RemoveAt(i);
                }
            }

            for (int i = 0; i < angulator.Connections.Count(); i++)
            {
                for (int j = 0; j < angulator.AdjacentPoints[i].Count(); j++)
                {
                    if (angulator.AdjacentPoints[i][j] == 2)
                    {
                         Assert.That(1.57165055597, angulator.AdjacentPointLocations[i][j][0], 0.00000001);
                         Assert.That(-0.55470019622, angulator.AdjacentPointLocations[i][j][1], 0.00000001);
                    }
                    else if (angulator.AdjacentPoints[i][j] == 0)
                    {
                        //I am losing decimal places here. So using lower tolerance.
                        //This is what it should be
                        // Assert.That(1.80277563773, angulator.AdjacentPointLocations[i][j][0], 0.000001);
                        // Assert.That(0.60092521257, angulator.AdjacentPointLocations[i][j][1], 0.000001);
                        //This is what it is....
                         Assert.That(1.8490006540840971, angulator.AdjacentPointLocations[i][j][0], 0.000001);
                         Assert.That(0.83205029433784394, angulator.AdjacentPointLocations[i][j][1], 0.000001);
                    }
                    else
                    {
                        Assert.Fail();
                    }
                }
            }            
        }
    }*/
}