public class Input : PersistentSingleton<Input>
{
    public static float H, V, RawH, RawV;
    public static bool MoveDown;
    public static bool JumpDown, JumpUp;
    public static bool WallPress;
    public static bool DashDown;
    void Update()
    {
        H         = UnityEngine.Input.GetAxis("Horizontal");
        V         = UnityEngine.Input.GetAxis("Vertical");
        RawH      = UnityEngine.Input.GetAxisRaw("Horizontal");
        RawV      = UnityEngine.Input.GetAxisRaw("Vertical");
        MoveDown  = UnityEngine.Input.GetButtonDown("Horizontal");
        JumpDown  = UnityEngine.Input.GetButtonDown("Jump");
        JumpUp    = UnityEngine.Input.GetButtonUp("Jump");
        WallPress = UnityEngine.Input.GetButton("Wall");
        DashDown  = UnityEngine.Input.GetButtonDown("Dash");
    }
}