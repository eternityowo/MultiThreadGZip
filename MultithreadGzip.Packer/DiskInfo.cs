using System.IO;
using System.Runtime.InteropServices;

namespace MultithreadGzip.Packer
{
	internal static class DiskInfo
    {
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetDiskFreeSpaceW")]
		static extern bool GetDiskFreeSpace(string lpRootPathName,
									  out int lpSectorsPerCluster,
									  out int lpBytesPerSector,
									  out int lpNumberOfFreeClusters,
									  out int lpTotalNumberOfClusters);

		public static int GetClusterSize(string path)
		{
			int clusterSize = 0;

			if (GetDiskFreeSpace(Path.GetPathRoot(path),
						out int sectorsPerCluster,
						out int bytesPerSector,
						out int freeClusters,
						out int totalClusters))
			{
				clusterSize = bytesPerSector * sectorsPerCluster;
			}
			return clusterSize;
		}
	}
}
