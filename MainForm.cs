﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using OneLevelJson.Export;
using OneLevelJson.Model;
using OneLevelJson.TexturePacker;

namespace OneLevelJson
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            _document = new Document();

            // TODO 임시로 처리 해둔것. save를 구현하면 수정해야함.
            Document.ProjectDirectory = Application.StartupPath;

            InitDocument();

            AddEvent();
        }

        private void InitDocument()
        {
            blackboard.PresentDocument = _document;
            ReloadAssetList();
            ReloadComponentList();
            ReloadLayerList();
            blackboard.Invalidate();

            // TODO 분리해주어야 좋을 Directory 설정. 이 부분을 어디서 사용할지 모르니까 쉽사리 분리를 하지 못하겠다.
            string projectPath = Document.ProjectDirectory ?? Application.StartupPath;
            MakeDirectory(projectPath + @"\" + Document.Name);
            MakeDirectory(projectPath + @"\" + Document.Name + @"\" + AssetDirectory);
            MakeDirectory(projectPath + @"\" + Document.Name + @"\" + ImageDataDirectory);
        }

        private void NewDocument(string name, int width, int height)
        {
            _document = new Document(name, width, height);
            InitDocument();
        }

        private void LoadDocument(string dir)
        {
            string extension = dir.Split('.').Last();
            if (extension != ProjectExtension)
            {
                MessageBox.Show(@"프로젝트 파일이 아닙니다!");
                return;
            }

            string docstring = File.ReadAllText(dir);
            ParseDocument(docstring);

            InitDocument();
        }

        private void SaveDocument(string filename)
        {
            string docjson = JsonConvert.SerializeObject(_document);
            File.WriteAllText(Document.ProjectDirectory + @"\" + filename, docjson);
            // _document를 json으로 serialize해서 파일에 쓰기.
        }

        private void ParseDocument(string docstring)
        {
            // docstring을 deserialize해서 doc에 넣어주기.
            _document = JsonConvert.DeserializeObject<Document>(docstring);
        }

        private void ImportAsset(string[] files)
        {
            // 1. 이미지들을 실행파일이 있는 프로젝트로 복사한 후,
            foreach (var file in files)
            {
                string projectDirectory = Document.ProjectDirectory ?? Application.StartupPath;

                try
                {
                    File.Copy(file, projectDirectory + @"\" + Document.Name + @"\"
                                    + ImageDataDirectory + @"\" + file.Split('\\').Last());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Asset newAsset = MakeAssetFrom(file);
                _document.AddAsset(newAsset);
            }

            // 3. 현재 AssetList를 다시 로드한다.
            ReloadAssetList();
        }

        private void Export(string exportDir)
        {
            string imagePackDir = exportDir + @"\orig";
            MakeDirectory(imagePackDir);

            // 1. TexturePacker로 기본적인 이미지를 만든다.
            TexturePacker.LoadAssets(_document.Assets);
            TexturePacker.RunPacking();
            TexturePacker.MakePackImage(imagePackDir);
            TexturePacker.MakeAtlas(imagePackDir);

            // 2. project.dt, scene.dt를 만든다.
            ModelMaker.Extract(_document);
            ModelMaker.Make();
        }

        private Asset MakeAssetFrom(string file)
        {
            AssetType type;

            string extension = file.Split('.')[0];
            string name = file.Split('\\').Last();
            switch (extension)
            {
                case "png":
                case "PNG":
                case "jpg":
                case "JPG":
                    type = AssetType.Image;
                    break;
                default:
                    type = AssetType.Image;
                    break;
            }
            return new Asset(type, name);
        }

        private void ReloadAssetList()
        {
            // 1. assetList를 비운다.
            assetList.BeginUpdate();
            assetList.Clear();
            assetImageList.Images.Clear();

            // 2. Document.Assets의 목록대로 assetList를 만든다.
            int listCounter = 0;
            foreach (var asset in _document.Assets)
            {
                // 2-1. ListViewItem을 만든다.
                ListViewItem lvi = new ListViewItem(asset.GetName())
                {
                    ImageIndex = listCounter++
                };
                // 2-2. 이미지를 ImageList에 추가한다.
                assetImageList.Images.Add(MakeImageFrom(asset.GetNameWithExt()));

                // 2-3. ListView에 추가한다.
                assetList.Items.Add(lvi);
            }
            assetList.EndUpdate();
        }

        private void ReloadComponentList()
        {
            componentList.BeginUpdate();
            componentList.Clear();

            foreach (var component in _document.Components)
            {
                componentList.Items.Add(new ListViewItem(component.Id));
            }

            componentList.EndUpdate();
        }

        private void ReloadLayerList()
        {
            layerList.BeginUpdate();
            layerList.Clear();

            foreach (var layer in _document.Layers)
            {
                layerList.Items.Add(new ListViewItem(layer.Name));
            }

            layerList.EndUpdate();
        }

        private void AddComponent(ListView.SelectedListViewItemCollection items, Point location)
        {
            for (int i = 0; i < items.Count; i++)
            {
                string name = items[i].Text;
                Size offset = new Size(15*i, 15*i);
                _document.AddComponent(name, location + offset);
            }

            ReloadComponentList();
        }

        private Image MakeImageFrom(string imageName)
        {
            string projectDirectory = Document.ProjectDirectory ?? Application.StartupPath;
            string newImagename = imageName;

            if (!CheckExt(imageName))
            {
                Asset asset = _document.Assets.Find(x => x.GetName() == imageName);
                newImagename = asset.GetNameWithExt();
            }

            return Image.FromFile(projectDirectory + @"\" + Document.Name + @"\"
                                  + ImageDataDirectory + @"\" + newImagename);
        }

        public void MakeDirectory(string dir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            if (!dirInfo.Exists) Directory.CreateDirectory(dir);
        }

        public bool CheckExt(string name)
        {
            return name.Split('.').Length > 1;
        }

        /************************************************************************/
        /* Event Callback                                                       */
        /************************************************************************/

        private void AddEvent()
        {
            assetList.SelectedIndexChanged += assetList_SelectedIndexChanged;
            assetList.ItemActivate += assetList_ItemActivate;
            assetList.ItemDrag += assetList_ItemDrag;
            assetList.DragOver += assetList_DragOver;
            assetList.DragEnter += assetList_DragEnter;

            componentList.MouseDown += componentList_MouseDown;

            layerList.SelectedIndexChanged += layerList_SelectedIndexChanged;
            layerList.MouseDown += layerList_MouseDown;

            blackboard.DragEnter += blackboard_DragEnter;
            blackboard.DragDrop += blackboard_DragDrop;
            blackboard.KeyDown += blackboard_KeyDown;
        }

        /************************************************************************/
        /* Asset List															*/
        /************************************************************************/

        private void assetList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (componentList.SelectedItems.Count != 0)
            {
                string selectedId = assetList.SelectedItems[0].Text;
                Asset selectedComponent = _document.Assets.Find(x => x.GetName() == selectedId);
                picBox.Image = MakeImageFrom(selectedComponent.GetNameWithExt());
            }
        }

        private void assetList_ItemActivate(object sender, EventArgs e)
        {
            MessageBox.Show(@"File Information");
        }

        private void assetList_ItemDrag(object sender, ItemDragEventArgs e)
        {
            State.log.Write("DRAG START");
            //            assetList.DoDragDrop(e.Item, DragDropEffects.Move); // start dragging
            assetList.DoDragDrop(assetList.SelectedItems, DragDropEffects.Move); // start dragging

            // the code below will run after the end of dragging
            State.log.Write("DRAG END");
        }

        private void assetList_DragEnter(object sender, DragEventArgs e)
        {
            State.log.Write("DRAG ENTER");
            e.Effect = e.AllowedEffect;
        }

        private void assetList_DragOver(object sender, DragEventArgs e)
        {
            State.log.Write(e.X + " " + e.Y);
        }

        /************************************************************************/
        /* Component List														*/
        /************************************************************************/

        private void componentList_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListViewHitTestInfo hitTestInfo = componentList.HitTest(e.X, e.Y);
                if (hitTestInfo.Item == null) return;

                if (componentList.SelectedIndices.Count > 1)
                {
                    componentMenuStrip.Items[0].Enabled = false;
                }
                componentMenuStrip.Show(this, componentList.Location + (Size) e.Location);
            }
        }

        private void componentRenameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ContextRenameForm renameForm = new ContextRenameForm();
            if (renameForm.ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem selectedItem in componentList.SelectedItems)
                {
                    string selectedId = selectedItem.Text;
                    string newId = renameForm.Result;
                    _document.RenameComponent(selectedId, newId);
                }
            }
            ReloadComponentList();
        }

        private void conponentRemoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (componentList.SelectedItems.Count > 0)
            {
                for (int i = componentList.SelectedItems.Count - 1; i >= 0; i--)
                {
                    ListViewItem item = componentList.SelectedItems[i];
                    componentList.Items[item.Index].Remove();
                    _document.RemoveComponent(item.SubItems[0].Text);
                }
            }
            blackboard.Invalidate();
        }

        /************************************************************************/
        /* Layer List															*/
        /************************************************************************/

        private void addLayer_Click(object sender, EventArgs e)
        {
            _document.Layers.Add(new Model.Layer("layer" + _document.Layers.Count, false));
            ReloadLayerList();
        }

        private void deleteLayer_Click(object sender, EventArgs e)
        {
            var items = layerList.SelectedItems;
            if (items.Count == 0) return;

            for (int i = 0; i < items.Count; i++)
            {
                Model.Layer selectedLayer = _document.Layers.Find(x => x.Name == items[i].Text);
                _document.Layers.Remove(selectedLayer);
            }

            ReloadLayerList();
        }

        private void layerList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (layerList.SelectedItems.Count == 0)
            {
                State.SelectedLayer = null;
            }
            else if (layerList.SelectedItems.Count == 1)
            {
                State.SelectedLayer = _document.Layers.Find(x => x.Name == layerList.SelectedItems[0].Text);
            }
        }

        private void layerList_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListViewHitTestInfo hitTestInfo = layerList.HitTest(e.X, e.Y);
                if (hitTestInfo.Item == null) return;

                layerMenuStrip.Show(this, layerList.Location + (Size) e.Location);
            }
        }

        private void layerRenameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ContextRenameForm renameForm = new ContextRenameForm();
            if (renameForm.ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem selectedItem in layerList.SelectedItems)
                {
                    string selectedId = selectedItem.Text;
                    string newId = renameForm.Result;
                    _document.RenameLayer(selectedId, newId);
                }
            }
            ReloadLayerList();
        }

        /************************************************************************/
        /* Blackboard															*/
        /************************************************************************/

        private void blackboard_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof (ListView.SelectedListViewItemCollection)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void blackboard_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof (ListView.SelectedListViewItemCollection))) return;

            var items =
                e.Data.GetData(typeof (ListView.SelectedListViewItemCollection)) as
                    ListView.SelectedListViewItemCollection;

            if (items == null) return;
            picBox.Image = MakeImageFrom(items[0].Text); // 미리보기 이미지 설정

            Point location = blackboard.PointToClient(new Point(e.X, e.Y));

            AddComponent(items, location);
            blackboard.Invalidate();
        }

        private void blackboard_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    if (blackboard.SelectedComponent != null)
                    {
                        _document.RemoveComponent(blackboard.SelectedComponent.Id);
                        blackboard.RemoveSelected();
                    }
                    break;
            }
            blackboard.Invalidate();
        }

        /************************************************************************/
        /* Menu Strip															*/
        /************************************************************************/

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (imageImportDialog.ShowDialog())
            {
                case DialogResult.OK:
                    ImportAsset(imageImportDialog.FileNames); // for debug
                    break;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (openProjectDialog.ShowDialog())
            {
                case DialogResult.OK:
                    LoadDocument(openProjectDialog.FileNames[0]); // for debug
                    break;
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewForm newForm = new NewForm();
            if (newForm.ShowDialog() == DialogResult.OK)
            {
                string name = newForm.Name;
                int width = newForm.Width;
                int height = newForm.Height;
                NewDocument(name, width, height);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO 일단 프로젝트 저장(Save)은 프로그램이 있는 위치에 하도록 하자.
            /*if (Document.SaveDirectory == null && saveFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string newDir = selectedPath + @"\" + Document.Name;
                MakeDirectory(newDir);
                Document.SaveDirectory = newDir;
                Directory.Move(Application.StartupPath + AssetDirectory, Document.SaveDirectory + AssetDirectory);
            }*/

            Document.ProjectDirectory = Application.StartupPath;
            InitDocument();
            SaveDocument(Document.Name + "." + ProjectExtension);

            MessageBox.Show(Document.Name + @" 프로젝트가 저장되었습니다.");
        }

        private void jsonExportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Document.ExportDirectory == null && exportFolderBrowser.ShowDialog() == DialogResult.OK)
            {
                Document.ExportDirectory = exportFolderBrowser.SelectedPath;
                MakeDirectory(Document.ExportDirectory + @"\scenes");
                Export(Document.ExportDirectory);
            }
        }

        /************************************************************************/
        /* DEBUG																*/
        /************************************************************************/

        /************************************************************************/
        /* Variables															*/
        /************************************************************************/
        private Document _document;
        public readonly Packer TexturePacker = new Packer();
        public readonly Maker ModelMaker = new Maker();
        private const string ProjectExtension = "dt";
        public const string AssetDirectory = @"\assets";
        public const string ImageDataDirectory = @"\assets\image";

        private void tESTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //string src = @"C:\Users\HAJIN\Documents\Visual Studio 2013\Projects\OneLevelJson\bin\Debug\assets";
            //string dst = @"C:\Users\HAJIN\Desktop" + @"\" + Document.Name + AssetDirectory;
            //MakeDirectory(@"C:\Users\HAJIN\Desktop\noname");
            //Directory.Move(src, dst);
        }
    }
}