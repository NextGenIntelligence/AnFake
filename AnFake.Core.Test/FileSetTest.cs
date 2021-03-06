﻿using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[DeploymentItem("Data/FileSet", "Data/FileSet")]
	[TestClass]
	public class FileSetTest
	{		
		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_include_exact_file()
		{
			// arrange
			var fs = "file-1.txt".AsFileSetFrom("Data/FileSet");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(1, files.Count);
			Assert.AreEqual("FiLe-1.txt", files[0].RelPath.Spec);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_include_by_full_path()
		{
			// arrange
			var fs = Path.GetFullPath("Data/FileSet/file-1.txt").AsFileSetFrom("Data/FileSet");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(1, files.Count);
			Assert.AreEqual("FiLe-1.txt", files[0].RelPath.Spec);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_include_by_file_pattern()
		{
			// arrange
			var fs = "*.txt".AsFileSetFrom("Data/FileSet");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(2, files.Count);
			Assert.AreEqual("FiLe-1.txt", files[0].RelPath.Spec);
			Assert.AreEqual("fIlE-2.Txt", files[1].RelPath.Spec);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_include_all_files_1()
		{
			// arrange
			var fs = "*".AsFileSetFrom("Data/FileSet");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(3, files.Count);
			Assert.AreEqual("FiLe-1.txt", files[0].RelPath.Spec);
			Assert.AreEqual("fIlE-2.Txt", files[1].RelPath.Spec);
			Assert.AreEqual("FILE-3.js", files[2].RelPath.Spec);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_include_all_files_2()
		{
			// arrange
			var fs = "*.*".AsFileSetFrom("Data/FileSet");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(3, files.Count);
			Assert.AreEqual("FiLe-1.txt", files[0].RelPath.Spec);
			Assert.AreEqual("fIlE-2.Txt", files[1].RelPath.Spec);
			Assert.AreEqual("FILE-3.js", files[2].RelPath.Spec);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_exclude_by_file_pattern()
		{
			// arrange
			var fs = "*".AsFileSetFrom("Data/FileSet") - "*.txt";

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(1, files.Count);
			Assert.AreEqual("FILE-3.js", files[0].RelPath.Spec);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_include_by_dir_pattern()
		{
			// arrange
			var fs = "*/dir-*/*".AsFileSetFrom("Data/FileSet");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(2, files.Count);
			Assert.AreEqual("dir-A\\dir-C\\file-6.txt", files[0].RelPath.Spec);
			Assert.AreEqual("dir-B\\dir-D\\file-7.txt", files[1].RelPath.Spec);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_exclude_by_dir_pattern()
		{
			// arrange
			var fs = "*/dir-*./*".AsFileSetFrom("Data/FileSet") - "*/dir-D/*";

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(1, files.Count);
			Assert.AreEqual("dir-A\\dir-C\\file-6.txt", files[0].RelPath.Spec);			
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_include_in_different_base_dirs()
		{
			// arrange
			var fs = "*".AsFileSetFrom("Data/FileSet/dir-A/dir-C") +
				"*".AsFileSetFrom("Data/FileSet/dir-B/dir-D");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(2, files.Count);
			Assert.AreEqual("file-6.txt", files[0].RelPath.Spec);
			Assert.AreEqual("file-7.txt", files[1].RelPath.Spec);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_include_recursively()
		{
			// arrange
			var fs = "**/*".AsFileSetFrom("Data/FileSet");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(9, files.Count);
			Assert.IsTrue(files.Any(x => x.RelPath.Spec == "FiLe-1.txt"));
			Assert.IsTrue(files.Any(x => x.RelPath.Spec == "dir-B\\dir-D\\file-7.txt"));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_exclude_subfolders()
		{
			// arrange
			var fs = "**/*".AsFileSetFrom("Data/FileSet") - "**/xdir-?/*";

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(7, files.Count);
			Assert.IsTrue(files.Any(x => x.RelPath.Spec == "FiLe-1.txt"));
			Assert.IsTrue(files.Any(x => x.RelPath.Spec == "dir-B\\dir-D\\file-7.txt"));
			Assert.IsFalse(files.Any(x => x.RelPath.Spec == "dir-A\\xdir-E\\file-8.txt"));
			Assert.IsFalse(files.Any(x => x.RelPath.Spec == "dir-A\\dir-C\\xdir-F\\file-9.txt"));
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_support_wildcard_in_base_path()
		{
			// arrange
			var fs = "*".AsFileSetFrom("Data/FileSet/*/dir-?");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(2, files.Count);
			Assert.AreEqual("file-6.txt", files[0].RelPath.Spec);
			Assert.AreEqual("file-7.txt", files[1].RelPath.Spec);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_support_wildcard_steps()
		{
			// arrange
			var fs = "Data/FileSet/*/dir-?/*".AsFileSet();

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(2, files.Count);
			Assert.AreEqual("Data\\FileSet\\dir-A\\dir-C\\file-6.txt", files[0].RelPath.Spec);
			Assert.AreEqual("Data\\FileSet\\dir-B\\dir-D\\file-7.txt", files[1].RelPath.Spec);
		}		

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_unc_pathes()
		{
			// arrange
			var fs = "//127.0.0.1/c$/*".AsFileSet();

			// act
			var files = fs.ToList();

			// assert
			Assert.IsTrue(files.Count >= 1);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_preserve_order_1()
		{
			// arrange
			var fs = "file-2.txt".AsFileSetFrom("Data/FileSet")
				.Include("file-3.js")
				.Include("file-1.txt");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(3, files.Count);
			Assert.AreEqual("fIlE-2.Txt", files[0].RelPath.Spec);
			Assert.AreEqual("FILE-3.js", files[1].RelPath.Spec);
			Assert.AreEqual("FiLe-1.txt", files[2].RelPath.Spec);			
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_preserve_order_2()
		{
			// arrange
			var fs = "file-3.js".AsFileSetFrom("Data/FileSet")
				.Include("file-1.txt")
				.Include("file-2.txt");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(3, files.Count);
			Assert.AreEqual("FILE-3.js", files[0].RelPath.Spec);
			Assert.AreEqual("FiLe-1.txt", files[1].RelPath.Spec);
			Assert.AreEqual("fIlE-2.Txt", files[2].RelPath.Spec);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_accepts_ending_double_asterisks()
		{
			// arrange
			var fs = "Data/FileSet/**".AsFileSet();

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(9, files.Count);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void FileSet_should_accepts_ending_double_asterisks_in_base_path()
		{
			// arrange
			var fs = "*".AsFileSetFrom("Data/FileSet/**");

			// act
			var files = fs.ToList();

			// assert
			Assert.AreEqual(6, files.Count);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSetToString_should_return_no_files_if_empty()
		{
			// arrange
			var fs = new FileSet();

			// act
			var s = fs.ToString();

			// assert
			Assert.AreEqual("(no files)", s);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSetToString_should_return_pattern_wo_basepath()
		{
			// arrange
			var fs = "Data/*".AsFileSet();

			// act
			var s = fs.ToString();

			// assert
			Assert.AreEqual("!!'Data\\*'", s);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSetToString_should_return_pattern_w_basepath()
		{
			// arrange
			var fs = "*.txt".AsFileSetFrom("Data");

			// act
			var s = fs.ToString();

			// assert
			Assert.AreEqual("'Data' % '*.txt'", s);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSetToString_should_return_multi_patterns_wo_basepath()
		{
			// arrange
			var fs = "dir-A/*.txt".AsFileSet() + "dir-B/*.doc";

			// act
			var s = fs.ToString();

			// assert
			Assert.AreEqual("!!'dir-A\\*.txt' + !!'dir-B\\*.doc'", s);
		}

		[TestCategory("Unit")]
		[TestMethod]
		public void FileSetToString_should_return_multi_patterns_w_basepath()
		{
			// arrange
			var fs = "*.txt".AsFileSetFrom("dir-A") + "*.doc".AsFileSetFrom("dir-B");

			// act
			var s = fs.ToString();

			// assert
			Assert.AreEqual("'dir-A' % '*.txt' + 'dir-B' % '*.doc'", s);
		}
	}
}