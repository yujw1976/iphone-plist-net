﻿/* =================================================================================
 * File:   PListDict.cs
 * Author: Christian Ecker
 *
 * Major Changes:
 * yyyy-mm-dd   Author               Description
 * ----------------------------------------------------------------
 * 2009-09-13   Christian Ecker      Created
 *
 * =================================================================================
 * Copyright (c) 2009, Christian Ecker
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 * 
 *  - Redistributions of source code must retain the above copyright notice, 
 *    this list of conditions and the following disclaimer.
 * 
 *  - Redistributions in binary form must reproduce the above copyright notice, 
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *    
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE 
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
 * THE POSSIBILITY OF SUCH DAMAGE.
 * =================================================================================
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Schema;

using CE.iPhone.PList.Internal;

namespace CE.iPhone.PList {

    /// <summary>
    /// Represents an dictionary with <see cref="T:System.String"/> keys and <see cref="T:CE.iPhone.IPListElement"/> values
    /// </summary>
    public class PListDict : Dictionary<String, IPListElement>, IPListElement {
        #region IPListElement Members

        /// <summary>
        /// Gets the Xml tag of this element.
        /// </summary>
        /// <value>The Xml tag of this element.</value>
        public String Tag { get { return "dict"; } }

        /// <summary>
        /// Gets the binary typecode of this element.
        /// </summary>
        /// <value>The binary typecode of this element.</value>
        public Byte TypeCode { get { return 0x0D; } }

        /// <summary>
        /// Reads this element binary from the reader.
        /// </summary>
        /// <param name="reader">The <see cref="T:CE.iPhone.PListBinaryReader"/> from which the element is read.</param>
        /// <remarks>Provided for internal use only.</remarks>
        public void ReadBinary(PListBinaryReader reader) {
            Byte[] bufKeys = new Byte[reader.CurrentElementLength * reader.ElementIdxSize];
            Byte[] bufVals = new Byte[reader.CurrentElementLength * reader.ElementIdxSize];
            Debug.Assert(reader.BaseStream.Read(bufKeys, 0, bufKeys.Length) == bufKeys.Length);
            Debug.Assert(reader.BaseStream.Read(bufVals, 0, bufVals.Length) == bufVals.Length);

            

            for (int i = 0; i < reader.CurrentElementLength; i++) {
                IPListElement plKey = reader.ReadInternal(reader.ElementIdxSize == 1 ?
                    bufKeys[i] : IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bufKeys, 2 * i)));

                Debug.Assert(plKey is PListString, "Key is no String");

                IPListElement plVal = reader.ReadInternal(reader.ElementIdxSize == 1 ?
                    bufVals[i] : IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bufVals, 2 * i)));
                
                Add( (PListString)plKey, plVal);
            }
        }

        /// <summary>
        /// Gets the length of this PList element.
        /// </summary>
        /// <returns>The length of this PList element.</returns>
        /// <remarks>Provided for internal use only.</remarks>
        public int GetPListElementLength() {
            return this.Count;
        }

        /// <summary>
        /// Gets the count of PList elements in this element.
        /// </summary>
        /// <returns>
        /// The count of PList elements in this element.
        /// </returns>
        /// <remarks>Provided for internal use only.</remarks>
        public int GetPListElementCount() {
            int count = 1;
            foreach (var item in this.Values) {
                count += item.GetPListElementCount();
            }
            count += this.Keys.Count;
            return count;
        }

        /// <summary>
        /// Writes this element binary to the writer.
        /// </summary>
        /// <param name="writer">The <see cref="T:CE.iPhone.PListBinaryWriter"/> to which the element is written.</param>
        /// <remarks>Provided for internal use only.</remarks>
        public void WriteBinary(PListBinaryWriter writer) {
            Byte[] keys = new Byte[writer.ElementIdxSize * Count];
            Byte[] values = new Byte[writer.ElementIdxSize * Count];
            long streamPos = writer.BaseStream.Position;
            writer.BaseStream.Write(keys, 0, keys.Length);
            writer.BaseStream.Write(values, 0, values.Length);

            KeyValuePair<String, IPListElement>[] elems = this.ToArray();

            for (int i = 0; i < Count; i++) {
                int elementIdx = writer.WriteInternal(PListElementFactory.Instance.CreateKeyElement(elems[i].Key));
                writer.FormatIdx(elementIdx).CopyTo(keys, writer.ElementIdxSize * i);
            }
            for (int i = 0; i < Count; i++) {
                int elementIdx = writer.WriteInternal(elems[i].Value);
                writer.FormatIdx(elementIdx).CopyTo(values, writer.ElementIdxSize * i);
            }

            writer.BaseStream.Seek(streamPos, SeekOrigin.Begin);
            writer.BaseStream.Write(keys, 0, keys.Length);
            writer.BaseStream.Write(values, 0, values.Length);
            writer.BaseStream.Seek(0, SeekOrigin.End);
        }
        #endregion

        #region IXmlSerializable Members

        /// <summary>
        /// This method is reserved and should not be used. When implementing the IXmlSerializable interface, you should return null (Nothing in Visual Basic) from this method, and instead, if specifying a custom schema is required, apply the <see cref="T:System.Xml.Serialization.XmlSchemaProviderAttribute"/> to the class.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Xml.Schema.XmlSchema"/> that describes the XML representation of the object that is produced by the <see cref="M:System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter)"/> method and consumed by the <see cref="M:System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader)"/> method.
        /// </returns>
        public XmlSchema GetSchema() { return null; }

        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader"/> stream from which the object is deserialized.</param>
        public void ReadXml(XmlReader reader) {

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
                return;


            while (reader.NodeType != System.Xml.XmlNodeType.EndElement) {
                reader.ReadStartElement("key");
                String key = reader.ReadString();
                reader.ReadEndElement();

                IPListElement plelem = PListElementFactory.Instance.Create(reader.LocalName);
                plelem.ReadXml(reader);
                this.Add(key, plelem);

                reader.MoveToContent();
            }

            reader.ReadEndElement();
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter"/> stream to which the object is serialized.</param>
        public void WriteXml(XmlWriter writer) {
            writer.WriteStartElement(Tag);
            foreach (var key in this.Keys) {
                writer.WriteStartElement("key");
                writer.WriteValue(key);
                writer.WriteEndElement();

                this[key].WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        #endregion
    }
}