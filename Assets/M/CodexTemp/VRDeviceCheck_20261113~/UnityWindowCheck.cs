using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

// Native-window diagnostics only. This ignored-by-Unity folder has no runtime dependencies.
public static class UnityWindowCheck
{
    [StructLayout(LayoutKind.Sequential)] struct Bounds { public int Left, Top, Right, Bottom; }
    public sealed class MenuEntry { public string Path; public uint Id, State; }
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window,int command);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint attach,uint target,bool value);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] static extern IntPtr GetMenu(IntPtr window);
    [DllImport("user32.dll")] static extern IntPtr GetSubMenu(IntPtr menu, int index);
    [DllImport("user32.dll")] static extern int GetMenuItemCount(IntPtr menu);
    [DllImport("user32.dll")] static extern uint GetMenuItemID(IntPtr menu, int index);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetMenuString(IntPtr menu, uint item, StringBuilder text, int count, uint flags);
    [DllImport("user32.dll")] static extern uint GetMenuState(IntPtr menu, uint item, uint flags);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window, out Bounds bounds);
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr window, IntPtr context, uint flags);

    static IntPtr Validate(long handle)
    {
        var window=new IntPtr(handle);var title=new StringBuilder(1024);uint process;
        GetWindowText(window,title,title.Capacity);GetWindowThreadProcessId(window,out process);
        if(!IsWindow(window)||!title.ToString().StartsWith("ProjectWait - Wait_Robby - ")||
            Process.GetProcessById((int)process).ProcessName!="Unity")
            throw new InvalidOperationException("Observed clean Wait_Robby editor window is no longer available; no input sent.");
        return window;
    }
    public static MenuEntry[] Menus(long handle)
    {
        var entries=new List<MenuEntry>();Walk(GetMenu(Validate(handle)),"",entries);return entries.ToArray();
    }
    public static void Restore(long handle)
    {
        var window=Validate(handle);if(IsIconic(window))ShowWindow(window,9);
    }
    public static bool Focus(long handle)
    {
        var window=Validate(handle);if(IsIconic(window))ShowWindow(window,9);
        uint process;uint foreground=GetWindowThreadProcessId(GetForegroundWindow(),out process),current=GetCurrentThreadId();
        bool attached=foreground!=current&&AttachThreadInput(current,foreground,true);
        try{return SetForegroundWindow(window);}finally{if(attached)AttachThreadInput(current,foreground,false);}
    }
    public static void RefreshAssetsAfterObservedStopped(long handle)
    {
        var window=Validate(handle);
        foreach(var entry in Menus(handle))
            if(entry.Path.Replace("&","").StartsWith("Assets/Refresh\t")&&(entry.State&3)==0)
            {
                if(!PostMessage(window,0x111,new IntPtr(entry.Id),IntPtr.Zero))throw new InvalidOperationException("Refresh menu invocation failed.");
                return;
            }
        throw new InvalidOperationException("Refresh menu unavailable; no input sent.");
    }
    static void Walk(IntPtr menu,string parent,List<MenuEntry> entries)
    {
        for(int i=0;i<GetMenuItemCount(menu);i++)
        {
            var text=new StringBuilder(512);GetMenuString(menu,(uint)i,text,text.Capacity,0x400);
            string path=parent+text.ToString();
            entries.Add(new MenuEntry{Path=path,Id=GetMenuItemID(menu,i),State=GetMenuState(menu,(uint)i,0x400)});
            var child=GetSubMenu(menu,i);if(child!=IntPtr.Zero)Walk(child,path+"/",entries);
        }
    }
    // Call only after observing the grey Play triangle; native menu state alone is not sufficient.
    public static void StartPlayAfterObservedStopped(long handle)
    {
        var window=Validate(handle);MenuEntry play=null;
        foreach(var entry in Menus(handle))
            if(entry.Path.Replace("&","").StartsWith("Edit/Play Mode/Play\t"))play=entry;
        if(play==null)throw new InvalidOperationException("Native Play menu not found; no input sent.");
        if((play.State&3)!=0)throw new InvalidOperationException("Play is disabled; no input sent.");
        if((play.State&8)!=0)throw new InvalidOperationException("Already in Play Mode; no toggle sent.");
        if(!PostMessage(window,0x111,new IntPtr(play.Id),IntPtr.Zero))throw new InvalidOperationException("Play menu invocation failed.");
    }
    // Unity's native menu is not reliably checked in Play Mode. Only call after observing the blue Stop button.
    public static void StopPlayAfterObservedRunning(long handle)
    {
        var window=Validate(handle);
        foreach(var entry in Menus(handle))
            if(entry.Path.Replace("&","").StartsWith("Edit/Play Mode/Play\t")&&(entry.State&3)==0)
            {
                if(!PostMessage(window,0x111,new IntPtr(entry.Id),IntPtr.Zero))throw new InvalidOperationException("Stop menu invocation failed.");
                return;
            }
        throw new InvalidOperationException("Play menu unavailable; no input sent.");
    }
    public static void Capture(long handle,string path)
    {
        var window=Validate(handle);Bounds bounds;
        string allowed=Path.GetFullPath(@"D:\project\ProjectWait\Assets\M\CodexTemp\VRDeviceCheck_20261113~\");
        if(!Path.GetFullPath(path).StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Capture must stay in the M diagnostic folder.");
        if(!GetWindowRect(window,out bounds))throw new InvalidOperationException("No window bounds.");
        using(var image=new Bitmap(bounds.Right-bounds.Left,bounds.Bottom-bounds.Top))
        using(var graphics=Graphics.FromImage(image))
        {
            var context=graphics.GetHdc();
            try{if(!PrintWindow(window,context,2))throw new InvalidOperationException("Capture failed.");}
            finally{graphics.ReleaseHdc(context);}
            image.Save(path,ImageFormat.Png);
        }
    }
}
