namespace TestLibrary;

/// <summary>
/// Test class remark.
/// </summary>
public class TestClass
{
    /// <summary>
    /// Test method remark.
    /// </summary>
    public void TestMethod()
    {
    }
    
    /// <summary>
    /// Test field remark.
    /// </summary>
    public int TestField;
    
    /// <summary>
    /// Test property remark.
    /// </summary>
    public int TestProperty { get; set; }
    
    /// <summary>
    /// method remark.
    /// </summary>
    /// <param name="a">a1</param>
    /// <param name="b">b1</param>
    /// <param name="c">c1</param>
    public void TestMethod2(int a, ref int b, out int c)
    {
        c = 0;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public int TestMethod3(int a, ref int b, out int c)
    {
        c = 0;
        return 0;
    }
}

/// <summary>
/// Test class remark.
/// AndMore
/// <br/>More...
/// </summary>
public class TestClass2
{
    /// <summary>
    /// Test method remark.
    /// AndMore
    /// <br/>More...
    /// </summary>
    public void TestMethod()
    {
    }
    
    /// <summary>
    /// Test field remark.
    /// AndMore
    /// <br/>More...
    /// </summary>
    public int TestField;
    
    /// <summary>
    /// Test property remark.
    /// AndMore
    /// <br/>More...
    /// </summary>
    public int TestProperty { get; set; }
    
    // UnImportant Remark
    /*
     * UnImportant Multi  Remark
     */
    public int TestField2;
}

public class TestList : List<int>{

}