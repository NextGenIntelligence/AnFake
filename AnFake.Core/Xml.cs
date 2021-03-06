﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	/// <summary>
	///		Represents XML document manipulation tools.
	/// </summary>
	public static class Xml
	{
		/// <summary>
		///		Represents loaded XML document.
		/// </summary>
		public sealed class XDoc
		{
			private readonly FileItem _file;
			private readonly System.Xml.Linq.XDocument _doc;
			private readonly XmlNamespaceManager _ns;

			internal XDoc(FileItem file, System.Xml.Linq.XDocument doc, XmlNamespaceManager ns)
			{
				_file = file;
				_doc = doc;
				_ns = ns;
			}

			/// <summary>
			///		XML document root element.
			/// </summary>
			public XNode Root
			{
				get { return new XNode(_doc.Root, _ns); }
			}

			public Encoding DeclaredEncoding
			{
				get
				{
					var enc = _doc.Declaration.Encoding;
					
					if ("utf-8".Equals(enc, StringComparison.OrdinalIgnoreCase))
						return Encoding.UTF8;

					if ("utf-16".Equals(enc, StringComparison.OrdinalIgnoreCase))
						return Encoding.Unicode;

					throw new InvalidConfigurationException(String.Format("Unsupported XML encoding: '{0}'.", enc));
				}

				set
				{
					if (value == null)
						throw new ArgumentException("XDoc.DeclaredEncoding: value must not be null");

					if (value.EncodingName == Encoding.UTF8.EncodingName)
					{
						_doc.Declaration.Encoding = "utf-8";
						return;
					}

					if (value.EncodingName == Encoding.Unicode.EncodingName)
					{
						_doc.Declaration.Encoding = "utf-16";
						return;
					}

					throw new InvalidConfigurationException(String.Format("Unsupported XML encoding: '{0}'.", value.EncodingName));
				}
			}

			/// <summary>
			///		Adds namespace mapping.
			/// </summary>
			/// <param name="prefix">namespace prefix (not null)</param>
			/// <param name="uri">namespace URI (not null)</param>
			/// <returns>XDoc</returns>
			public XDoc Ns(string prefix, string uri)
			{
				if (prefix == null)
					throw new ArgumentException("XDoc.Ns(prefix, uri): prefix must not be null");
				if (uri == null)
					throw new ArgumentException("XDoc.Ns(prefix, uri): uri must not be null");

				_ns.AddNamespace(prefix, uri);
				return this;
			}

			/// <summary>
			///		Selects XML document elements by xpath.
			/// </summary>
			/// <remarks>
			///		Methods searchers over elements only. You can not select an attribute, CDATA or others in this way, 
			///		but it is possible to get attribute value with a single xpath by <c>ValueOf</c> method.
			/// </remarks>
			/// <param name="xpath">xpath (not null)</param>
			/// <returns>sequence of XML document elements</returns>
			public IEnumerable<XNode> Select(string xpath)
			{
				if (xpath == null)
					throw new ArgumentException("XDoc.Select(xpath): xpath must not be null");

				return _doc.XPathSelectElements(xpath, _ns)
					.Select(x => new XNode(x, _ns));
			}

			/// <summary>
			///		Selects first XML document element matched by xpath or throws an exception if no one.
			/// </summary>
			/// <remarks>
			///		See <see cref="Select"/>.
			/// </remarks>
			/// <param name="xpath">xpath (not null)</param>
			/// <returns>matched XML document element</returns>
			public XNode SelectFirst(string xpath)
			{
				if (xpath == null)
					throw new ArgumentException("XDoc.SelectFirst(xpath): xpath must not be null");

				var subNode = _doc.XPathSelectElement(xpath, _ns);
				if (subNode == null)
					throw new InvalidConfigurationException(String.Format("Node '{0}' not found in XML document.", xpath));

				return new XNode(subNode, _ns);
			}

			/// <summary>
			///		Returns value of XML document node matched by xpath.
			/// </summary>
			/// <remarks>
			///		XPath might match any valid XML node including attribute, CDATA, etc.
			/// </remarks>
			/// <param name="xpath">xpath (not null)</param>
			/// <param name="defaultValue">default value to be returned if no one node matched</param>
			/// <returns>value of matched XML node</returns>
			public string ValueOf(string xpath, string defaultValue = "")
			{
				if (xpath == null)
					throw new ArgumentException("XDoc.ValueOf(xpath): xpath must not be null");

				var nav = _doc.CreateNavigator(_ns.NameTable);
				var subNode = nav.SelectSingleNode(xpath, _ns);
				return subNode == null
					? defaultValue
					: subNode.Value;
			}

			/// <summary>
			///		Saves XML document to the same file which it was loaded. Throws an exception if document was loaded from stream.
			/// </summary>
			public void Save()
			{
				if (_file == null)
					throw new InvalidConfigurationException("Unable to save XML document because it wasn't loaded from file. Hint: use SaveTo method instead.");

				_doc.Save(_file.Path.Full);
			}

			/// <summary>
			///		Saves XML document to specified file.
			/// </summary>
			/// <param name="file">file to save document to (not null)</param>
			public void SaveTo(FileItem file)
			{
				if (file == null)
					throw new ArgumentException("XDoc.Save(file): file must not be null");

				file.EnsurePath();

				_doc.Save(file.Path.Full);
			}

			/// <summary>
			///		Returns XML document as string without declaration.
			/// </summary>
			/// <remarks>
			///		IMPORTANT! This method returns xml string WITHOUT '&lt;?xml version encoding?&gt;' declaration. 
			///		If you are going to save returned string to the file use <c>ToStringWithDeclaration</c> instead.
			/// </remarks>
			/// <returns>xml string</returns>
			public override string ToString()
			{
				return _doc.ToString();
			}

			/// <summary>
			///		Returns XML document as string with declaration.
			/// </summary>
			/// <remarks>
			///		IMPORTANT! This method returns xml string with '&lt;?xml version encoding?&gt;' declaration. Encoding is defined by <c>DeclaredEncoding</c> property.
			/// </remarks>
			/// <returns>xml string</returns>
			/// <example>
			/// <code>
			/// let xml = "&lt;root/&gt;".AsXmlDoc()
			/// xml.DeclaredEncoding = Encoding.Unicode			
			/// let s = xml.ToStringWithDeclaration()	// &lt;?xml version="1.0" encoding="utf-16"?&gt;
			/// </code>
			/// </example>
			public string ToStringWithDeclaration()
			{
				return 
					new StringBuilder()
						.AppendLine(_doc.Declaration.ToString())
						.Append(_doc)
						.ToString();
			}
		}

		/// <summary>
		///		Represents node inside XML document.
		/// </summary>
		public sealed class XNode
		{
			private readonly System.Xml.Linq.XElement _element;
			private readonly XmlNamespaceManager _ns;

			internal XNode(System.Xml.Linq.XElement element, XmlNamespaceManager ns)
			{
				_element = element;
				_ns = ns;
			}

			/// <summary>
			///		Adds namespace mapping.
			/// </summary>
			/// <param name="prefix">namespace prefix (not null)</param>
			/// <param name="uri">namespace URI (not null)</param>
			/// <returns>XNode</returns>
			public XNode Ns(string prefix, string uri)
			{
				if (prefix == null)
					throw new ArgumentException("XNode.Ns(prefix, uri): prefix must not be null");
				if (uri == null)
					throw new ArgumentException("XNode.Ns(prefix, uri): uri must not be null");

				_ns.AddNamespace(prefix, uri);
				return this;
			}

			/// <summary>
			///		Returns attribute value.
			/// </summary>
			/// <remarks>
			///		Use prefixes mapped via <c>Ns</c> method to specify full attribute name.
			/// </remarks>
			/// <param name="name">attribute name (not null)</param>
			/// <param name="defaultValue">default value to be returned if no such attribute</param>
			/// <returns>attribute value</returns>
			public string Attr(string name, string defaultValue = "")
			{
				if (name == null)
					throw new ArgumentException("XNode.Attr(name): name must not be null");

				var attr = _element.Attribute(ToXName(name));
				return attr != null ? attr.Value : defaultValue;
			}

			/// <summary>
			///		Sets attribute value.
			/// </summary>
			/// <remarks>
			///		Use prefixes mapped via <c>Ns</c> method to specify full attribute name.
			/// </remarks>
			/// <param name="name">attribute name (not null)</param>
			/// <param name="value">attribute value</param>
			public void SetAttr(string name, string value)
			{
				if (name == null)
					throw new ArgumentException("XNode.SetAttr(name, value): name must not be null");

				_element.SetAttributeValue(ToXName(name), value);
			}

			/// <summary>
			///		Returns current node value.
			/// </summary>
			/// <returns>node value</returns>
			public string Value()
			{
				return _element.Value;
			}

			/// <summary>
			///		Sets current node value.
			/// </summary>
			/// <param name="value">value to be set</param>
			public void SetValue(string value)
			{
				_element.SetValue(value);
			}

			/// <summary>
			///		Returns value of XML document node matched by xpath.
			/// </summary>
			/// <remarks>
			///		XPath might match any valid XML node including attribute, CDATA, etc.
			/// </remarks>
			/// <param name="xpath">xpath (not null)</param>
			/// <param name="defaultValue">default value to be returned if no one node matched</param>
			/// <returns>value of matched XML node</returns>
			public string ValueOf(string xpath, string defaultValue = "")
			{
				if (xpath == null)
					throw new ArgumentException("XNode.ValueOf(xpath[, defaultValue]): xpath must not be null");

				var nav = _element.CreateNavigator(_ns.NameTable);
				var subNode = nav.SelectSingleNode(xpath, _ns);
				return subNode == null
					? defaultValue
					: subNode.Value;
			}

			/// <summary>
			///		Selects XML document elements by xpath.
			/// </summary>
			/// <remarks>
			///		Methods searchers over elements only. You can not select an attribute, CDATA or others in this way, 
			///		but it is possible to get attribute value with a single xpath by <c>ValueOf</c> method.
			/// </remarks>
			/// <param name="xpath">xpath (not null)</param>
			/// <returns>sequence of XML document elements</returns>
			public IEnumerable<XNode> Select(string xpath)
			{
				if (xpath == null)
					throw new ArgumentException("XNode.Select(xpath): xpath must not be null");

				return _element.XPathSelectElements(xpath, _ns)
					.Select(x => new XNode(x, _ns));
			}

			/// <summary>
			///		Selects first XML document element matched by xpath or throws an exception if no one.
			/// </summary>
			/// <remarks>
			///		See <see cref="Select"/>.
			/// </remarks>
			/// <param name="xpath">xpath (not null)</param>
			/// <returns>matched XML document element</returns>
			public XNode SelectFirst(string xpath)
			{
				if (xpath == null)
					throw new ArgumentException("XNode.SelectFirst(xpath): xpath must not be null");

				var subNode = _element.XPathSelectElement(xpath, _ns);
				if (subNode == null)
					throw new InvalidConfigurationException(String.Format("Node '{0}' not found in XML document.", xpath));

				return new XNode(subNode, _ns);
			}

			/// <summary>
			///		Inserts new element before current one.
			/// </summary>
			/// <remarks>
			///		Use prefixes mapped via <c>Ns</c> method to specify full element name.
			/// </remarks>
			/// <param name="element">element name (not null)</param>
			/// <param name="value">element value</param>
			public void InsertBefore(string element, string value)
			{
				if (element == null)
					throw new ArgumentException("XNode.InsertBefore(element, value): element must not be null");

				_element.AddBeforeSelf(new System.Xml.Linq.XElement(ToXName(element), value));
			}

			/// <summary>
			///		Inserts new element after current one.
			/// </summary>
			/// <remarks>
			///		Use prefixes mapped via <c>Ns</c> method to specify full element name.
			/// </remarks>
			/// <param name="element">element name (not null)</param>
			/// <param name="value">element value</param>
			public void InsertAfter(string element, string value)
			{
				if (element == null)
					throw new ArgumentException("XNode.InsertAfter(element, value): element must not be null");

				_element.AddAfterSelf(new System.Xml.Linq.XElement(ToXName(element), value));
			}

			/// <summary>
			///		Appends new element as child of current one.
			/// </summary>
			/// <remarks>
			///		Use prefixes mapped via <c>Ns</c> method to specify full element name.
			/// </remarks>
			/// <param name="element">element name (not null)</param>
			/// <param name="value">element value</param>
			public void Append(string element, string value)
			{
				if (element == null)
					throw new ArgumentException("XNode.Append(element, value): element must not be null");

				_element.Add(new System.Xml.Linq.XElement(ToXName(element), value));
			}

			/// <summary>
			///		Appends new element as child of current one and returns element just added.
			/// </summary>
			/// <remarks>
			///		Use prefixes mapped via <c>Ns</c> method to specify full element name.
			/// </remarks>
			/// <param name="element">element name (not null)</param>			
			public XNode Append(string element)
			{
				if (element == null)
					throw new ArgumentException("XNode.Append(element): element must not be null");

				var xelem = new System.Xml.Linq.XElement(ToXName(element));
				_element.Add(xelem);

				return new XNode(xelem, _ns);
			}

			/// <summary>
			///		Returns string presentation of Xml element.
			/// </summary>
			/// <returns>xml string</returns>
			public override string ToString()
			{
				return _element.ToString();
			}

			private System.Xml.Linq.XName ToXName(string name)
			{
				var index = name.IndexOf(':');
				if (index < 0)
					return System.Xml.Linq.XName.Get(name, _ns.DefaultNamespace);

				var prefix = name.Substring(0, index);
				var uri = _ns.LookupNamespace(prefix);
				if (uri == null)
					throw new InvalidConfigurationException(String.Format("XML namespace '{0}' not registered. Hint: use Ns() method to map prefix to namespace URI.", prefix));

				return System.Xml.Linq.XName.Get(name.Substring(index + 1), uri);
			}
		}

		/// <summary>
		///		Loads XML document from given file.
		/// </summary>
		/// <remarks>
		///		Returned document might be saved to the same file with <c>XDoc.Save</c> method.
		/// </remarks>
		/// <param name="file">file to load xml from (not null)</param>
		/// <returns>XML document</returns>
		public static XDoc AsXmlDoc(this FileItem file)
		{
			if (file == null)
				throw new ArgumentException("Xml.AsXmlDoc(file): file must not be null");

			using (var reader = XmlReader.Create(file.Path.Full))
			{
				return LoadXDoc(reader, file);
			}
		}		

		/// <summary>
		///		Loads XML document from given string.
		/// </summary>
		/// <remarks>
		///		Returned document might be saved to the file or stream with <c>XDoc.SaveTo</c> method.
		/// </remarks>
		/// <param name="xml">string to load xml from (not null)</param>
		/// <returns>XML document</returns>
		public static XDoc AsXmlDoc(this string xml)
		{
			if (xml == null)
				throw new ArgumentException("Xml.AsXmlDoc(xml): xml must not be null");

			using (var reader = XmlReader.Create(new StringReader(xml)))
			{
				return LoadXDoc(reader);
			}
		}

		private static XDoc LoadXDoc(XmlReader reader, FileItem file = null)
		{
			var xdoc = System.Xml.Linq.XDocument.Load(reader, System.Xml.Linq.LoadOptions.PreserveWhitespace);

			// ReSharper disable once AssignNullToNotNullAttribute
			return new XDoc(file, xdoc, new XmlNamespaceManager(reader.NameTable));
		}

		/// <summary>
		///		Invokes given action for each node of XML document from specified sequence.
		/// </summary>
		/// <param name="nodes">nodes sequence</param>
		/// <param name="action">action to do</param>
		public static void ForEach(this IEnumerable<XNode> nodes, Action<XNode> action)
		{			
			foreach (var subNode in nodes)
			{
				action.Invoke(subNode);
			}
		}
	}
}