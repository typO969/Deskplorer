using System.Runtime.InteropServices;

namespace Deskplorer.Native
{
   internal static class User32
   {
      [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
      internal static extern uint ExtractIconEx(
         string szFileName,
         int nIconIndex,
         IntPtr[]? phiconLarge,
         IntPtr[]? phiconSmall,
         uint nIcons);

      [DllImport("user32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool DestroyIcon(IntPtr hIcon);
   }
}
