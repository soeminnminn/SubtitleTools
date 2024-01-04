using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Xaml;

namespace SubtitleTools.UI
{
    public class TableDocumentWriter : IDisposable
    {
        #region Variables
        protected static readonly NamespaceDeclaration xamlNamespace = new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x");

        private readonly XamlWriter _writer;
        #endregion

        #region Constructors
        public TableDocumentWriter()
        {
            _writer = new XamlObjectWriter(System.Windows.Markup.XamlReader.GetWpfSchemaContext());
        }
        #endregion

        #region Properties
        protected XamlSchemaContext SchemaContext
        {
            get => _writer.SchemaContext;
        }
        #endregion

        #region Methods

        #region Wrapper Methods
        protected XamlType WriteStartObject(Type type)
        {
            var xamlType = SchemaContext.GetXamlType(type);
            _writer.WriteStartObject(xamlType);
            return xamlType;
        }

        protected void WriteEndObject()
        {
            _writer.WriteEndObject();
        }

        protected void WriteObject(Type type, Action<XamlType> writeMembers, Action<XamlType> writeChildren)
        {
            var objectType = SchemaContext.GetXamlType(type);
            _writer.WriteStartObject(objectType);
            if (writeMembers != null)
            {
                writeMembers(objectType);
            }
            if (writeChildren != null)
            {
                writeChildren(objectType);
            }
            _writer.WriteEndObject();
        }

        protected void WriteAttribute(XamlType objectType, string memberName, object value)
        {
            var member = objectType.GetMember(memberName);

            _writer.WriteStartMember(member);
            _writer.WriteValue(value);
            _writer.WriteEndMember();
        }

        protected void WriteAttribute(XamlType objectType, DependencyProperty property, object value)
        {
            if (value == null) return;

            XamlMember member;

            if (property.OwnerType.IsAssignableFrom(objectType.UnderlyingType))
            {
                member = objectType.GetMember(property.Name);
            }
            else
            {
                var type = SchemaContext.GetXamlType(property.OwnerType);
                member = type.GetAttachableMember(property.Name);
            }

            if (_writer is XamlObjectWriter writer)
            {
                writer.WriteStartMember(member);
                writer.WriteValue(value);
                writer.WriteEndMember();
            }
            else
            {
                string strValue;
                if (value is string str)
                    strValue = str;
                else
                {
                    var converter = member.TypeConverter.ConverterInstance;
                    try
                    {
                        strValue = converter.ConvertToString(value);
                    }
                    catch (Exception)
                    {
                        strValue = value.ToString();
                    }
                }

                if (strValue != null)
                {
                    _writer.WriteStartMember(member);
                    _writer.WriteValue(strValue);
                    _writer.WriteEndMember();
                }
            }  
        }

        protected void WriteStartCollection(XamlType objectType, string memberName)
        {
            var member = objectType.GetMember(memberName);

            _writer.WriteStartMember(member);
            _writer.WriteGetObject();
            _writer.WriteStartMember(XamlLanguage.Items);
        }

        protected void WriteStartCollection(XamlType objectType, DependencyProperty property)
        {
            XamlMember member;
            if (property.OwnerType.IsAssignableFrom(objectType.UnderlyingType))
                member = objectType.GetMember(property.Name);
            else
            {
                var type = SchemaContext.GetXamlType(property.OwnerType);
                member = type.GetAttachableMember(property.Name);
            }

            _writer.WriteStartMember(member);
            _writer.WriteGetObject();
            _writer.WriteStartMember(XamlLanguage.Items);
        }

        protected void WriteEndCollection()
        {
            _writer.WriteEndMember();
            _writer.WriteEndObject();
            _writer.WriteEndMember();
        }

        protected void WriteCollection(XamlType objectType, string memberName, Action writeChildren)
        {
            var member = objectType.GetMember(memberName);

            _writer.WriteStartMember(member);
            _writer.WriteGetObject();
            _writer.WriteStartMember(XamlLanguage.Items);

            if (writeChildren != null)
            {
                writeChildren();
            }

            _writer.WriteEndMember();
            _writer.WriteEndObject();
            _writer.WriteEndMember();
        }

        protected void WriteCollection(XamlType objectType, DependencyProperty property, Action writeChildren)
        {
            XamlMember member;
            if (property.OwnerType.IsAssignableFrom(objectType.UnderlyingType))
                member = objectType.GetMember(property.Name);
            else
            {
                var type = SchemaContext.GetXamlType(property.OwnerType);
                member = type.GetAttachableMember(property.Name);
            }

            _writer.WriteStartMember(member);
            _writer.WriteGetObject();
            _writer.WriteStartMember(XamlLanguage.Items);

            if (writeChildren != null)
            {
                writeChildren();
            }

            _writer.WriteEndMember();
            _writer.WriteEndObject();
            _writer.WriteEndMember();
        }

        protected void WriteStartContent(XamlType objectType)
        {
            XamlMember member = objectType.ContentProperty;
            _writer.WriteStartMember(member);
            _writer.WriteGetObject();
            _writer.WriteStartMember(XamlLanguage.Items);
        }

        protected void WriteEndContent()
        {
            _writer.WriteEndMember();
            _writer.WriteEndObject();
            _writer.WriteEndMember();
        }

        protected void WriteContent(XamlType objectType, Action writeChildren)
        {
            XamlMember member = objectType.ContentProperty;
            _writer.WriteStartMember(member);
            _writer.WriteGetObject();
            _writer.WriteStartMember(XamlLanguage.Items);

            if (writeChildren != null)
            {
                writeChildren();
            }

            _writer.WriteEndMember();
            _writer.WriteEndObject();
            _writer.WriteEndMember();
        }

        protected void WriteTextRun(string text)
        {
            var xamlType = SchemaContext.GetXamlType(typeof(Run));
            var member = xamlType.GetMember(nameof(Run.Text));

            _writer.WriteStartObject(xamlType);
            _writer.WriteStartMember(member);
            _writer.WriteValue(text);
            _writer.WriteEndMember();
            _writer.WriteEndObject();
        }
        #endregion

        private void WriteTableColumn(double width)
        {
            var columnType = WriteStartObject(typeof(TableColumn));
            WriteAttribute(columnType, TableColumn.WidthProperty, width);
            WriteEndObject(); // TableColumn
        }

        private void WriteCell(string text, TextAlignment textAlignment)
        {
            var cellType = WriteStartObject(typeof(TableCell));
            WriteStartContent(cellType); // TableCell.Content

            var paraType = WriteStartObject(typeof(Paragraph));
            WriteAttribute(paraType, Paragraph.PaddingProperty, 4);
            WriteAttribute(paraType, Paragraph.TextAlignmentProperty, textAlignment);

            WriteStartContent(paraType); // Paragraph.Content
            _writer.WriteValue(text);
            WriteEndContent(); // Paragraph.Content

            WriteEndObject(); // Paragraph

            WriteEndContent(); // TableCell.Content
            WriteEndObject(); // TableCell
        }

        private void WriteHorizontalRule(int columnsCount)
        {
            var rowType = WriteStartObject(typeof(TableRow));
            WriteAttribute(rowType, TableRow.FontSizeProperty, 0.2);
            WriteAttribute(rowType, TableRow.BackgroundProperty, SystemColors.ActiveBorderColor.ToString());

            WriteStartCollection(rowType, nameof(TableRow.Cells));

            var cellType = WriteStartObject(typeof(TableCell));
            WriteAttribute(cellType, TableCell.ColumnSpanProperty, columnsCount);
            WriteEndObject(); // TableCell

            WriteEndCollection(); // TableRow.Cells

            WriteEndObject(); // TableRow
        }

        public void Render(Column[] columns, IEnumerable<string[]> list)
        {
            _writer.WriteNamespace(xamlNamespace);

            var docType = WriteStartObject(typeof(FlowDocument));
            WriteAttribute(docType, FlowDocument.FontFamilyProperty, "Tahoma");

            WriteStartCollection(docType, nameof(FlowDocument.Blocks));

            var tableType = WriteStartObject(typeof(Table));
            WriteAttribute(tableType, Table.CellSpacingProperty, 0);

            // Table.Columns
            WriteCollection(tableType, nameof(Table.Columns), () => 
            {
                foreach (var column in columns)
                {
                    WriteTableColumn(column.Width);
                }
            });

            WriteStartCollection(tableType, nameof(Table.RowGroups));

            var rowGroupType = WriteStartObject(typeof(TableRowGroup));
            WriteStartCollection(rowGroupType, nameof(TableRowGroup.Rows));

            WriteHorizontalRule(columns.Length);

            var rowType = WriteStartObject(typeof(TableRow)); // [Header]
            WriteAttribute(rowType, TableRow.FontSizeProperty, 14.0);
            WriteAttribute(rowType, TableRow.FontWeightProperty, "SemiBold");

            // TableRow.Cells
            WriteCollection(rowType, nameof(TableRow.Cells), () => 
            {
                foreach (var column in columns)
                {
                    WriteCell(column.Header, column.HeaderAlignment);
                }
            });
            WriteEndObject(); // TableRow [Header]

            WriteHorizontalRule(columns.Length);

            using (var emu = list.GetEnumerator())
            {
                while(emu.MoveNext())
                {
                    var bodyRowType = WriteStartObject(typeof(TableRow));
                    WriteAttribute(bodyRowType, TableRow.FontSizeProperty, 12.0);

                    var cells = emu.Current;

                    // TableRow.Cells
                    WriteCollection(rowType, nameof(TableRow.Cells), () =>
                    {
                        for (int i = 0; i < columns.Length; i++)
                        {
                            if (cells.Length > i)
                            {
                                WriteCell(cells[i], columns[i].CellAlignment);
                            }
                            else
                            {
                                WriteCell(string.Empty, columns[i].CellAlignment);
                            }
                        }
                    });

                    WriteEndObject(); // TableRow
                }
            }

            WriteEndCollection(); // TableRowGroup.Rows
            WriteEndObject(); // TableRowGroup

            WriteEndCollection(); // Table.RowGroups

            WriteEndObject(); // Table
            WriteEndCollection(); // FlowDocument.Blocks
            WriteEndObject(); // FlowDocument
        }

        public object GetResult()
        {
            if (_writer is XamlObjectWriter ow)
            {
                return ow.Result;
            }

            return null;
        }

        public FlowDocument ToFlowDocument()
        {
            if (_writer is XamlObjectWriter ow)
            {
                return ow.Result as FlowDocument;
            }
            return null;
        }

        public void Save(TextWriter textWriter)
        {
            IDocumentPaginatorSource documentSource = ToFlowDocument();
            if (documentSource != null)
            {
                System.Windows.Markup.XamlWriter.Save(documentSource, textWriter);
            }
        }

        public void Save(System.Xml.XmlWriter xmlWriter)
        {
            IDocumentPaginatorSource documentSource = ToFlowDocument();
            if (documentSource != null)
            {
                System.Windows.Markup.XamlWriter.Save(documentSource, xmlWriter);
            }
        }

        public void Save(Stream stream)
        {
            IDocumentPaginatorSource documentSource = ToFlowDocument();
            if (documentSource != null)
            {
                System.Windows.Markup.XamlWriter.Save(documentSource, stream);
            }
        }

        public void Dispose()
        {
            _writer.Close();
        }
        #endregion

        #region Nested Types
        public struct Column
        {
            public string Header;
            public double Width;
            public TextAlignment HeaderAlignment;
            public TextAlignment CellAlignment;
        }
        #endregion
    }
}
