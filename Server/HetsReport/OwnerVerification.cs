﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HetsData.Helpers;
using HetsData.Model;
using HetsReport.Helpers;

namespace HetsReport
{
    public static class OwnerVerification
    {
        private const string ResourceName = "HetsReport.Templates.OwnerVerification-Template.docx";

        public static byte[] GetOwnerVerification(OwnerVerificationReportModel reportModel, string name)
        {
            try
            {
                // ******************************************************
                // get document template
                // ******************************************************
                Assembly assembly = Assembly.GetExecutingAssembly();
                byte[] byteArray = new byte[] { };
                int ownerCount = 0;

                using (Stream templateStream = assembly.GetManifestResourceStream(ResourceName))
                {
                    if (templateStream != null)
                    {
                        byteArray = new byte[templateStream.Length];
                        templateStream.Read(byteArray, 0, byteArray.Length);
                        templateStream.Close();
                    }
                }

                using (MemoryStream documentStream = new MemoryStream())
                {
                    WordprocessingDocument wordDocument = WordprocessingDocument.Create(documentStream, WordprocessingDocumentType.Document, true);

                    // add a main document part
                    wordDocument.AddMainDocumentPart();

                    using (MemoryStream templateStream = new MemoryStream())
                    {
                        templateStream.Write(byteArray, 0, byteArray.Length);
                        WordprocessingDocument wordTemplate = WordprocessingDocument.Open(templateStream, true);

                        if (wordTemplate == null)
                            throw new Exception("Owner Verification template not found");

                        // ******************************************************
                        // merge document content
                        // ******************************************************
                        foreach (HetOwner owner in reportModel.Owners)
                        {
                            ownerCount++;

                            using (MemoryStream ownerStream = new MemoryStream())
                            {
                                WordprocessingDocument ownerDocument = (WordprocessingDocument)wordTemplate.Clone(ownerStream);
                                ownerDocument.Save();

                                // update address and contact information
                                string[] addressLabels = GetAddressLabels(owner.OrganizationName, owner.DoingBusinessAs, owner.Address1, owner.Address2);
                                string[] addressInfo = GetAddressDetail(owner.OrganizationName, owner.DoingBusinessAs, owner.Address1, owner.Address2);

                                string[] contactLabels = GetContactLabels(owner.PrimaryContact.WorkPhoneNumber, owner.PrimaryContact.MobilePhoneNumber, owner.PrimaryContact.FaxPhoneNumber);
                                string[] contactInfo = GetContactDetail(owner.PrimaryContact.WorkPhoneNumber, owner.PrimaryContact.MobilePhoneNumber, owner.PrimaryContact.FaxPhoneNumber);

                                Dictionary<string, string> values = new Dictionary<string, string>
                                {
                                    { "classification", owner.Classification },
                                    { "districtAddress", reportModel.DistrictAddress },
                                    { "districtContact", reportModel.DistrictContact },
                                    { "addressLabels0", addressLabels[0]},
                                    { "addressLabels1", addressLabels[1]},
                                    { "addressLabels2", addressLabels[2]},
                                    { "addressLabels3", addressLabels[3]},
                                    { "addressInfo0", addressInfo[0]},
                                    { "addressInfo1", addressInfo[1]},
                                    { "addressInfo2", addressInfo[2]},
                                    { "addressInfo3", addressInfo[3]},
                                    { "reportDate", reportModel.ReportDate },
                                    { "ownerCode", owner.OwnerCode },
                                    { "sharedKeyHeader", owner.SharedKeyHeader },
                                    { "sharedKey", owner.SharedKey },
                                    { "contactLabels0", contactLabels[0]},
                                    { "contactLabels1", contactLabels[1]},
                                    { "contactLabels2", contactLabels[2]},
                                    { "contactInfo0", contactInfo[0]},
                                    { "contactInfo1", contactInfo[1]},
                                    { "contactInfo2", contactInfo[2]}
                                };

                                // update classification number first [ClassificationNumber]
                                owner.Classification = owner.Classification.Replace("&", "&amp;");
                                bool found = false;

                                foreach (OpenXmlElement paragraphs in ownerDocument.MainDocumentPart.Document.Body.Elements())
                                {
                                    foreach (OpenXmlElement paragraphRun in paragraphs.Elements())
                                    {
                                        foreach (OpenXmlElement text in paragraphRun.Elements())
                                        {
                                            if (text.InnerText.Contains("ClassificationNumber"))
                                            {
                                                // replace text
                                                text.InnerXml = text.InnerXml.Replace("<w:t>ClassificationNumber</w:t>",
                                                    $"<w:t xml:space='preserve'>ORCS: {owner.Classification}</w:t>");

                                                found = true;
                                                break;
                                            }
                                        }

                                        if (found) break;
                                    }

                                    if (found) break;
                                }

                                ownerDocument.MainDocumentPart.Document.Save();
                                ownerDocument.Save();

                                // update merge fields
                                MergeHelper.ConvertFieldCodes(ownerDocument.MainDocumentPart.Document);
                                MergeHelper.MergeFieldsInElement(values, ownerDocument.MainDocumentPart.Document);
                                ownerDocument.MainDocumentPart.Document.Save();

                                // setup table for equipment data
                                Table equipmentTable = GenerateEquipmentTable(owner.HetEquipment);
                                Paragraph tableParagraph = null;
                                found = false;

                                foreach (OpenXmlElement paragraphs in ownerDocument.MainDocumentPart.Document.Body.Elements())
                                {
                                    foreach (OpenXmlElement paragraphRun in paragraphs.Elements())
                                    {
                                        foreach (OpenXmlElement text in paragraphRun.Elements())
                                        {
                                            if (text.InnerText.Contains("Owner Equipment Table"))
                                            {
                                                // insert table here...
                                                text.RemoveAllChildren();
                                                tableParagraph = (Paragraph)paragraphRun.Parent;
                                                found = true;
                                                break;
                                            }
                                        }

                                        if (found) break;
                                    }

                                    if (found) break;
                                }

                                // append table to document
                                if (tableParagraph != null)
                                {
                                    Run run = tableParagraph.AppendChild(new Run());
                                    run.AppendChild(equipmentTable);
                                }

                                ownerDocument.MainDocumentPart.Document.Save();
                                ownerDocument.Save();

                                // merge owner into the master document
                                if (ownerCount == 1)
                                {
                                    // update document header
                                    foreach (HeaderPart headerPart in ownerDocument.MainDocumentPart.HeaderParts)
                                    {
                                        MergeHelper.ConvertFieldCodes(headerPart.Header);
                                        MergeHelper.MergeFieldsInElement(values, headerPart.Header);
                                        headerPart.Header.Save();
                                    }

                                    wordDocument = (WordprocessingDocument) ownerDocument.Clone(documentStream);

                                    ownerDocument.Close();
                                    ownerDocument.Dispose();
                                }
                                else
                                {
                                    // DELETE document header from owner document
                                    ownerDocument.MainDocumentPart.DeleteParts(ownerDocument.MainDocumentPart.HeaderParts);

                                    List<HeaderReference> headers = ownerDocument.MainDocumentPart.Document.Descendants<HeaderReference>().ToList();

                                    foreach (HeaderReference header in headers)
                                    {
                                        header.Remove();
                                    }

                                    // DELETE document footers from owner document
                                    ownerDocument.MainDocumentPart.DeleteParts(ownerDocument.MainDocumentPart.FooterParts);

                                    List<FooterReference> footers = ownerDocument.MainDocumentPart.Document.Descendants<FooterReference>().ToList();

                                    foreach (FooterReference footer in footers)
                                    {
                                        footer.Remove();
                                    }

                                    // DELETE section properties from owner document
                                    List<SectionProperties> properties = ownerDocument.MainDocumentPart.Document.Descendants<SectionProperties>().ToList();

                                    foreach (SectionProperties property in properties)
                                    {
                                        property.Remove();
                                    }

                                    ownerDocument.Save();

                                    // insert section break in master
                                    MainDocumentPart mainPart = wordDocument.MainDocumentPart;

                                    Paragraph para = new Paragraph();
                                    SectionProperties sectProp = new SectionProperties();
                                    SectionType secSbType = new SectionType() { Val = SectionMarkValues.OddPage };
                                    PageSize pageSize = new PageSize() { Width = 11900U, Height = 16840U, Orient = PageOrientationValues.Portrait };
                                    PageMargin pageMargin = new PageMargin() { Top = 2642, Right = 23U, Bottom = 278, Left = 23U, Header = 714, Footer = 0, Gutter = 0};

                                    // page numbering throws out the "odd page" section breaks
                                    //PageNumberType pageNum = new PageNumberType() {Start = 1};

                                    sectProp.AppendChild(secSbType);
                                    sectProp.AppendChild(pageSize);
                                    sectProp.AppendChild(pageMargin);
                                    //sectProp.AppendChild(pageNum);

                                    ParagraphProperties paragraphProperties = new ParagraphProperties(sectProp);
                                    para.AppendChild(paragraphProperties);

                                    mainPart.Document.Body.InsertAfter(para, mainPart.Document.Body.LastChild);
                                    mainPart.Document.Save();

                                    // append document body
                                    string altChunkId = $"AltChunkId{ownerCount}";

                                    AlternativeFormatImportPart chunk = mainPart.AddAlternativeFormatImportPart(AlternativeFormatImportPartType.WordprocessingML, altChunkId);

                                    ownerDocument.Close();
                                    ownerDocument.Dispose();

                                    ownerStream.Seek(0, SeekOrigin.Begin);
                                    chunk.FeedData(ownerStream);

                                    AltChunk altChunk = new AltChunk {Id = altChunkId };

                                    Paragraph para3 = new Paragraph();
                                    Run run3 = para3.InsertAfter(new Run(), para3.LastChild);
                                    run3.AppendChild(altChunk);

                                    mainPart.Document.Body.InsertAfter(para3, mainPart.Document.Body.LastChild);
                                    mainPart.Document.Save();
                                }
                            }
                        }

                        wordTemplate.Close();
                        wordTemplate.Dispose();
                        templateStream.Close();
                    }

                    // ******************************************************
                    // secure & return completed document
                    // ******************************************************
                    wordDocument.CompressionOption = CompressionOption.Maximum;
                    SecurityHelper.PasswordProtect(wordDocument);

                    wordDocument.Close();
                    wordDocument.Dispose();

                    documentStream.Seek(0, SeekOrigin.Begin);
                    byteArray = documentStream.ToArray();
                }

                return byteArray;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static Table GenerateEquipmentTable(IEnumerable<HetEquipment> equipmentList)
        {
            try
            {
                // create an empty table
                Table table = new Table();

                TableProperties tableProperties1 = new TableProperties();
                TableStyle tableStyle1 = new TableStyle() { Val = "TableGrid" };
                TableLayout tableLayout1 = new TableLayout() { Type = TableLayoutValues.Fixed };

                TableLook tableLook1 = new TableLook()
                {
                    Val = "04A0",
                    FirstRow = true,
                    LastRow = false,
                    FirstColumn = true,
                    LastColumn = false,
                    NoHorizontalBand = false,
                    NoVerticalBand = true
                };

                tableProperties1.AppendChild(tableLayout1);
                tableProperties1.AppendChild(tableStyle1);
                tableProperties1.AppendChild(tableLook1);

                table.AppendChild(tableProperties1);

                // setup headers
                TableRow tableRow1 = new TableRow();

                TableRowProperties rowProperties = new TableRowProperties();

                rowProperties.AppendChild(new TableRowHeight { Val = 200, HeightType = HeightRuleValues.AtLeast });
                rowProperties.AppendChild(new TableHeader { Val = OnOffOnlyValues.On });

                tableRow1.AppendChild(rowProperties);

                // add columns
                string col1Width = "1100";
                string col2Width = "1260";
                string col3Width = "1260";
                string col4Width = "2200";
                string col5Width = "2700";
                string col6Width = "1800";
                string col7Width = "1500";

                GridColumn gc1 = new GridColumn { Width = col1Width };
                tableRow1.AppendChild(gc1);
                tableRow1.AppendChild(SetupHeaderCell("Still own/ Re-register?", col1Width, true));

                GridColumn gc2 = new GridColumn { Width = col2Width };
                tableRow1.AppendChild(gc2);
                tableRow1.AppendChild(SetupHeaderCell("Local Area", col2Width, true));

                GridColumn gc3 = new GridColumn { Width = col3Width };
                tableRow1.AppendChild(gc3);
                tableRow1.AppendChild(SetupHeaderCell("Equipment Id", col3Width, true));

                GridColumn gc4 = new GridColumn { Width = col4Width };
                tableRow1.AppendChild(gc4);
                tableRow1.AppendChild(SetupHeaderCell("Equipment Type", col4Width));

                GridColumn gc5 = new GridColumn { Width = col5Width };
                tableRow1.AppendChild(gc5);
                tableRow1.AppendChild(SetupHeaderCell("Year/Make/Model/Serial Number/Size", col5Width));

                GridColumn gc6 = new GridColumn { Width = col6Width };
                tableRow1.AppendChild(gc6);
                tableRow1.AppendChild(SetupHeaderCell("Attachments", col6Width));

                GridColumn gc7 = new GridColumn { Width = col7Width };
                tableRow1.AppendChild(gc7);
                tableRow1.AppendChild(SetupHeaderCell("Owner Comments (sold, retired, etc.)", col7Width, true));

                table.AppendChild(tableRow1);

                // add rows for each equipment record
                foreach (HetEquipment equipment in equipmentList)
                {
                    TableRow tableRowEquipment = new TableRow();

                    TableRowProperties equipmentRowProperties = new TableRowProperties();
                    equipmentRowProperties.AppendChild(new TableRowHeight { Val = 200, HeightType = HeightRuleValues.AtLeast });
                    tableRowEquipment.AppendChild(equipmentRowProperties);

                    // add equipment data
                    tableRowEquipment.AppendChild(SetupCell("Yes   No", col1Width, true));
                    tableRowEquipment.AppendChild(SetupCell(equipment.LocalArea.Name, col2Width, true));
                    tableRowEquipment.AppendChild(SetupCell(equipment.EquipmentCode, col3Width, true));
                    tableRowEquipment.AppendChild(SetupCell(equipment.DistrictEquipmentType.DistrictEquipmentName, col4Width));

                    string temp = $"{equipment.Year} / {equipment.Make} / {equipment.Model} / {equipment.SerialNumber} / {equipment.Size}";
                    tableRowEquipment.AppendChild(SetupCell(temp, col5Width));

                    // attachments list
                    temp = "";
                    int row = 1;

                    foreach (HetEquipmentAttachment attachment in equipment.HetEquipmentAttachment)
                    {
                        temp = row == 1 ?
                            $"{attachment.Description}" :
                            $"{temp} / {attachment.Description}";

                        row++;
                    }
                    tableRowEquipment.AppendChild(SetupCell(temp, col6Width));

                    // last column (blank)
                    tableRowEquipment.AppendChild(SetupCell("", col7Width));

                    table.AppendChild(tableRowEquipment);
                }

                return table;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static TableCell SetupHeaderCell(string text, string width, bool center = false)
        {
            try
            {
                TableCell tableCell = new TableCell();

                TableCellProperties tableCellProperties = new TableCellProperties();
                TableCellWidth tableCellWidth = new TableCellWidth { Width = width, Type = TableWidthUnitValues.Dxa };
                Shading shading = new Shading { Val = ShadingPatternValues.Clear, Fill = "FFFFFF", Color = "auto" };

                // border & padding
                TableCellBorders borders = new TableCellBorders();

                TopBorder topBorder = new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Thick), Color = "A1A2A3" };
                borders.AppendChild(topBorder);

                BottomBorder bottomBorder = new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Thick), Color = "A1A2A3" };
                borders.AppendChild(bottomBorder);

                RightBorder rightBorder = new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) };
                borders.AppendChild(rightBorder);

                LeftBorder leftBorder = new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) };
                borders.AppendChild(leftBorder);

                TableCellMargin margin = new TableCellMargin();
                TopMargin topMargin = new TopMargin() { Width = "40" };
                BottomMargin bottomMargin = new BottomMargin() { Width = "40" };
                margin.AppendChild(topMargin);
                margin.AppendChild(bottomMargin);

                tableCellProperties.AppendChild(tableCellWidth);
                tableCellProperties.AppendChild(shading);
                tableCellProperties.AppendChild(borders);
                tableCellProperties.AppendChild(margin);

                tableCell.AppendChild(tableCellProperties);

                // add text (with specific formatting)
                Paragraph paragraph = new Paragraph() { RsidParagraphAddition = "00607D74", RsidRunAdditionDefault = "00607D74", ParagraphId = "6ED85602", TextId = "77777777" };

                ParagraphProperties paragraphProperties = new ParagraphProperties();
                ParagraphMarkRunProperties paragraphMarkRunProperties = new ParagraphMarkRunProperties();

                paragraphMarkRunProperties.AppendChild(new Color { Val = "000000" });
                paragraphMarkRunProperties.AppendChild(new RunFonts { Ascii = "Arial" });
                paragraphMarkRunProperties.AppendChild(new FontSize() { Val = "7pt" });
                paragraphMarkRunProperties.AppendChild(new Bold());

                Justification justification = new Justification() { Val = JustificationValues.Left };
                if (center) justification.Val = JustificationValues.Center;

                paragraphProperties.AppendChild(paragraphMarkRunProperties);
                paragraphProperties.AppendChild(justification);
                paragraph.AppendChild(paragraphProperties);

                paragraph.AppendChild(new Text(text));

                // add to table cell
                tableCell.AppendChild(paragraph);

                return tableCell;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static TableCell SetupCell(string text, string width, bool center = false)
        {
            try
            {
                TableCell tableCell = new TableCell();

                TableCellProperties tableCellProperties = new TableCellProperties();
                TableCellWidth tableCellWidth = new TableCellWidth() { Width = width, Type = TableWidthUnitValues.Dxa };

                // border & padding
                TableCellBorders borders = new TableCellBorders();

                TopBorder topBorder = new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Thick), Color = "A1A2A3" };
                borders.AppendChild(topBorder);

                BottomBorder bottomBorder = new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Thick), Color = "A1A2A3" };
                borders.AppendChild(bottomBorder);

                RightBorder rightBorder = new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) };
                borders.AppendChild(rightBorder);

                LeftBorder leftBorder = new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Nil) };
                borders.AppendChild(leftBorder);

                TableCellMargin margin = new TableCellMargin();
                TopMargin topMargin = new TopMargin() { Width = "40" };
                BottomMargin bottomMargin = new BottomMargin() { Width = "40" };
                margin.AppendChild(topMargin);
                margin.AppendChild(bottomMargin);

                tableCellProperties.AppendChild(tableCellWidth);
                tableCellProperties.AppendChild(borders);
                tableCellProperties.AppendChild(margin);

                tableCell.AppendChild(tableCellProperties);

                // add text (with specific formatting)
                Paragraph paragraph = new Paragraph() { RsidParagraphAddition = "00607D74", RsidRunAdditionDefault = "00607D74", ParagraphId = "6ED85602", TextId = "77777777" };

                ParagraphProperties paragraphProperties = new ParagraphProperties();
                ParagraphMarkRunProperties paragraphMarkRunProperties = new ParagraphMarkRunProperties();

                paragraphMarkRunProperties.AppendChild(new Color { Val = "000000" });
                paragraphMarkRunProperties.AppendChild(new RunFonts { Ascii = "Arial" });
                paragraphMarkRunProperties.AppendChild(new FontSize() { Val = "7pt" });

                Justification justification = new Justification() { Val = JustificationValues.Left };
                if (center) justification.Val = JustificationValues.Center;

                paragraphProperties.AppendChild(paragraphMarkRunProperties);
                paragraphProperties.AppendChild(justification);
                paragraph.AppendChild(paragraphProperties);

                paragraph.AppendChild(new Text(text));

                // add to table cell
                tableCell.AppendChild(paragraph);

                return tableCell;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static string[] GetAddressLabels(string businessName, string dbaName,
            string address1, string address2)
        {
            string[] temp = new string[4];

            if (!string.IsNullOrEmpty(businessName))
            {
                temp[0] = "Owner:";
            }

            if (!string.IsNullOrEmpty(dbaName))
            {
                if (string.IsNullOrEmpty(temp[0])) temp[0] = "Doing Business As:";
                else temp[1] = "Doing Business As:";
            }

            if (!string.IsNullOrEmpty(address1))
            {
                if (string.IsNullOrEmpty(temp[0])) temp[0] = "Address:";
                else if (string.IsNullOrEmpty(temp[1])) temp[1] = "Address:";
                else temp[2] = "Address:";
            }

            if (!string.IsNullOrEmpty(address2))
            {
                if (string.IsNullOrEmpty(temp[0])) temp[0] = " ";
                else if (string.IsNullOrEmpty(temp[1])) temp[1] = " ";
                else if (string.IsNullOrEmpty(temp[2])) temp[2] = " ";
                else temp[3] = " ";
            }

            return temp;
        }

        private static string[] GetAddressDetail(string businessName, string dbaName,
            string address1, string address2)
        {
            string[] temp = new string[4];

            if (!string.IsNullOrEmpty(businessName))
            {
                temp[0] = $"{businessName}";
            }

            if (!string.IsNullOrEmpty(dbaName))
            {
                if (string.IsNullOrEmpty(temp[0])) temp[0] = $"{dbaName}";
                else temp[1] = $"{dbaName}";
            }

            if (!string.IsNullOrEmpty(address1))
            {
                if (string.IsNullOrEmpty(temp[0])) temp[0] = $"{address1}";
                else if (string.IsNullOrEmpty(temp[1])) temp[1] = $"{address1}";
                else temp[2] = $"{address1}";
            }

            if (!string.IsNullOrEmpty(address2))
            {
                if (string.IsNullOrEmpty(temp[0])) temp[0] = $"{address2}";
                else if (string.IsNullOrEmpty(temp[1])) temp[1] = $"{address2}";
                else if (string.IsNullOrEmpty(temp[2])) temp[2] = $"{address2}";
                else temp[3] = $"{address2}";
            }

            return temp;
        }

        private static string[] GetContactLabels(string workPhoneNumber, string mobilePhoneNumber, string faxPhoneNumber)
        {
            string[] temp = new string[3];

            if (!string.IsNullOrEmpty(workPhoneNumber))
            {
                temp[0] = "Phone:";
            }

            if (!string.IsNullOrEmpty(mobilePhoneNumber))
            {
                if (string.IsNullOrEmpty(temp[0])) temp[0] = "Cell:";
                else temp[1] = "Cell:";
            }

            if (!string.IsNullOrEmpty(faxPhoneNumber))
            {
                if (string.IsNullOrEmpty(temp[0])) temp[0] = "Fax:";
                else if (string.IsNullOrEmpty(temp[1])) temp[1] = "Fax:";
                else temp[2] = "Fax:";
            }

            return temp;
        }

        private static string[] GetContactDetail(string workPhoneNumber, string mobilePhoneNumber, string faxPhoneNumber)
        {
            string[] temp = new string[3];

            if (!string.IsNullOrEmpty(workPhoneNumber))
            {
                temp[0] = $"{workPhoneNumber}";
            }

            if (!string.IsNullOrEmpty(mobilePhoneNumber))
            {
                if (string.IsNullOrEmpty(temp[0])) temp[0] = $"{mobilePhoneNumber}";
                else temp[1] = $"{mobilePhoneNumber}";
            }

            if (!string.IsNullOrEmpty(faxPhoneNumber))
            {
                if (string.IsNullOrEmpty(temp[0])) temp[0] = $"{faxPhoneNumber}";
                else if (string.IsNullOrEmpty(temp[1])) temp[1] = $"{faxPhoneNumber}";
                else temp[2] = $"{faxPhoneNumber}";
            }

            return temp;
        }
    }
}
