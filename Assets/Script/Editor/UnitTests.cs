using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class UnitTests {

    [Test]
    public void TryMoveTest()
    {
        CheckersBoard cb = new CheckersBoard(); //issues with Error: ECall methods must be packaged in a system module in both monodevelop and visual studio
        cb.TryMove(2, 2, 3, 3);
        //Console.WriteLine(cb.getSelectedPiece());
        Assert.IsFalse(!(cb.getStartDrag().Equals(Vector2.zero)));
        Assert.IsFalse(!(cb.getEndDrag().Equals(new Vector2(3, 3)))); // check if the end position is where it should be
        Assert.IsFalse(cb.getSelectedPiece() == null);
        return;
    }
		
    // A UnityTest behaves like a coroutine in PlayMode
    // and allows you to yield null to skip a frame in EditMode
    [UnityTest]
	public IEnumerator UnitTestsWithEnumeratorPasses() {
		// Use the Assert class to test conditions.
		// yield to skip a frame
		yield return null;
	}
}
