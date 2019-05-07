Option Strict On

Imports Intel.RealSense
Imports System.Threading 'Für Tasks zu beennden

Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Structure
Imports Emgu.CV.UI
Imports Emgu.CV.Util

Imports System.IO

'Infos:
'Emgu.CV.CvInvoke. zugriff auf alle openCV befehle

Public Class Form_Main

    'Konstanten
    Const cfpsDepth = 6 'Möglich: 6,15,30,60
    Const cfpsColor = 6

    Const cWidht = 1280
    Const cHeight = 720

    Const cMaxTrys = 3
    Const cMaxRestart = 3

    Const cCamOffset = 75 '107

    Dim ColorCamOffset As Int32

    Const Versatz_Bild_Tiefe_X = 100
    Const Versatz_Bild_Tiefe_Y = 100

    'Enums
    Public Enum Enum_Format
        color
        depth
    End Enum


    'Globale Variablen
    Private _MyPipeline As New Pipeline
    Private _MyCFG As New Config()
    Private _MyColorMap As New Colorizer()

    'TCPVariablen
    Private WithEvents _TcpVariablen As New TCPVariables


    'Konfigurations Variablen
    Private Konf_FPS_Color As Int32
    Private Konf_FPS_Depth As Int32

    Private Konf_Widht_Color As Int32
    Private Konf_Height_Color As Int32
    Private Konf_Widht_Depth As Int32
    Private Konf_Height_Depth As Int32

    Private Konf_Format_Color As Format
    Private Konf_Format_Depth As Format



    'Private _cts As New CancellationTokenSource '#### Für Tasks zu beennden
    Private _MyPipelineAktiv As Boolean
    Private _trys As Int32 = 0

    Private _DepthImgTaken As Boolean = False
    Private _DepthCImgTaken As Boolean = False
    Private _ColorImgTaken As Boolean = False
    Private _RefCImgTaken As Boolean = False
    Private _RefDImgTaken As Boolean = False

    'Listen
    Private _MyObjekte As New List(Of MyObjektV2)
    Private _MySearchObjekte As New List(Of MySearchObj)
    Private _MyMatchObjekts As New List(Of MyMatchObj)
    Private _MyRefXY_List As New List(Of MyRefObjekt)
    Private _MyRefZ_List As New List(Of MyRefObjekt)

    'Mat
    Private _MatColor As Mat
    Private _MatDepth As Mat
    Private _MatDepthC As Mat
    Private _MatRefC As Mat
    Private _MatRefD As Mat
    Private _MatRefDc As Mat
    Private _MatWatershedMask As New Mat
    Private _MatFound As New Mat
    Private _DisColor As New Mat
    Private _DisDepth As New Mat
    Private _DisDepthC As New Mat
    Private _DisRefC As New Mat
    Private _DisRefD As New Mat
    Private _DisRes01 As New Mat
    Private _DisRes02 As New Mat
    Private _DisFound As New Mat

    Private _MatResult As New Mat
    Private _MatPoints As New Mat


    '-----------------------------------------------------------------------------------------------------------------------
    'Constructor
    '-----------------------------------------------------------------------------------------------------------------------
    Sub New()

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()

        ' Fügen Sie Initialisierungen nach dem InitializeComponent()-Aufruf hinzu.

    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Laden & schliesen der Form
    '-----------------------------------------------------------------------------------------------------------------------
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Konfiguration vorbelegen
        'Farbe
        comb_Konf_Col_FPS.SelectedItem = "6" '6 Frames
        comb_Konf_Col_Format.SelectedItem = "BGR8" ' Format.Bgr8
        comb_Konf_Col_Auflösung.SelectedItem = "1280x720" '1280x720"

        'Tiefe
        comb_Konf_Dep_FPS.SelectedItem = "6" '6 Frames
        comb_Konf_Dep_Format.SelectedItem = "Z16" 'Format.Z16
        comb_Konf_Dep_Auflösung.SelectedItem = "1280x720" '1280x720"

        KonfigPipe()
        ImgStatus()
        LoadSettings()
        Autoreferenz()

        ColorCamOffset = CInt(num_CamOffset.Value) - cCamOffset

        'TCPVariablen anlegen
        _TcpVariablen.AddVariable("x")
        _TcpVariablen.AddVariable("y")
        _TcpVariablen.AddVariable("z")
        _TcpVariablen.AddVariable("h")
        _TcpVariablen.AddVariable("b")
        _TcpVariablen.AddVariable("t")
        _TcpVariablen.AddVariable("a")
        _TcpVariablen.AddVariable("pic")
        _TcpVariablen.AddVariable("rdy")
        _refreshDataGridView()
        'AddHandler btn_StartStop.Click, New EventHandler(AddressOf Me.ProcessFrameAndUpdateGUI)

    End Sub

    Private Sub Form1_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        'Steuerung
        My.Settings.Steur_Tolreanz = CInt(Num_SearchToleranz.Value)

        'Konfiguration
        My.Settings.Konf_MaskeH = CInt(num_MaskH.Value)
        My.Settings.Konf_MaskeV = CInt(num_MaskV.Value)
        My.Settings.Konf_KameraOffset = CInt(num_CamOffset.Value)
        My.Settings.Konf_KameraColorOffset = ColorCamOffset
        My.Settings.Konv_FaktorPixMilH = num_pixmmH_faktor.Value
        My.Settings.Konv_FaktorPixMilB = num_pixmmB_faktor.Value
        My.Settings.Konv_RoboOffsetX = num_RoboOffsetX.Value
        My.Settings.Konv_RoboOffsetY = num_RoboOffsety.Value
        'Auswertung
        My.Settings.Ausw_TiefsteReg = CInt(num_ThreshTief.Value)
        My.Settings.Ausw_HöchsteReg = CInt(num_ThreshHoch.Value)
        My.Settings.Ausw_MindestObjH = CInt(num_WTS_MinH.Value)
        My.Settings.Ausw_MindestObjB = CInt(num_WTS_MinB.Value)
        My.Settings.Ausw_MindestObjT = CInt(num_WTS_MinT.Value)
        'TCP
        My.Settings.TCP_Host = tb_TCP_HOST.Text
        My.Settings.TCP_Port = CInt(num_TCP_Port.Value)
        My.Settings.Save()

    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Button & Eingaben
    '-----------------------------------------------------------------------------------------------------------------------
    'Image
    Private Sub btn_NewImg_Click(sender As Object, e As EventArgs) Handles btn_NewImg.Click
        If TakePicture(_MatColor, _MatDepth, _MatDepthC) Then
            'Anzeigen
            CvInvoke.Resize(_MatColor, _DisColor, New Size(640, 480))
            ib_new_Color.Image = _DisColor.ToImage(Of Bgr, Byte)
            CvInvoke.Resize(_MatDepthC, _DisDepth, New Size(640, 480))
            ib_new_Depth.Image = _DisDepth.ToImage(Of Bgr, Byte)
            TC2_Bilder.SelectedTab = P1_NewImg

            TC2_Bilder.TabPages(5).Text = "Z16 Tiefenbild"
            ib_depth_01.Image = _MatDepth
        End If
    End Sub

    Private Sub btn_RefImg_Click(sender As Object, e As EventArgs) Handles btn_RefImg.Click
        If TakePicture(_MatRefC, _MatRefD, _MatRefDc) Then
            lb_Info.Items.Insert(0, "Referenzbilder Color erfolgreich")
            'Anzeigen
            CvInvoke.Resize(_MatRefC, _DisRefC, New Size(640, 480))
            ib_rev_Color.Image = _DisRefC.ToImage(Of Bgr, Byte)
            CvInvoke.Resize(_MatRefDc, _DisRefD, New Size(640, 480))
            ib_rev_Depth.Image = _DisRefD.ToImage(Of Bgr, Byte)
            TC2_Bilder.SelectedTab = P3_Rev

            TC2_Bilder.TabPages(5).Text = "Z16 Tiefenbild"
            ib_depth_01.Image = _MatDepth
        End If
    End Sub

    Private Sub btn_Reset_Click(sender As Object, e As EventArgs) Handles btn_Reset.Click
        lb_Info.Items.Insert(0, "Cam reset")
        RestartCam()
    End Sub

    Private Sub btn_Analyse_Click(sender As Object, e As EventArgs) Handles btn_Analyse.Click
        ImgageAnalyse()
    End Sub

    Private Sub btn_depthOffset_Click(sender As Object, e As EventArgs)

        TakePicture(_MatColor, _MatDepth, _MatDepthC)

    End Sub

    'Obj Search
    Private Sub btn_SearchObj_Click(sender As Object, e As EventArgs) Handles btn_SearchObj.Click
        Search()
    End Sub

    Private Sub btn_AddSearchObj_Click(sender As Object, e As EventArgs) Handles btn_AddSearchObj.Click
        lb_SearchObjList.Items.Clear()
        Dim tmpObj As New MySearchObj(_MySearchObjekte.Count + 1, tb_searchObjName.Text, CInt(num_newSearchObjH.Value), CInt(num_newSearchObjB.Value), CInt(num_newSearchObjT.Value))
        _MySearchObjekte.Add(tmpObj)

        For Each obj As MySearchObj In _MySearchObjekte
            lb_SearchObjList.Items.Add(obj.ToString())
        Next
    End Sub

    Private Sub btn_SaveSearch_Click(sender As Object, e As EventArgs) Handles btn_SaveSearch.Click
        Using SaveStreamerFile As New FileStream("SearchData.dat", FileMode.Create)
            Using SaveStreamWriter = New StreamWriter(SaveStreamerFile)
                For Each obj In _MySearchObjekte
                    'Write a line of text.
                    SaveStreamWriter.WriteLine(obj.GetSaveData())
                Next
                'Close the file.
                SaveStreamWriter.Close()
            End Using
        End Using

    End Sub

    Private Sub btn_LoadSearch_Click(sender As Object, e As EventArgs) Handles btn_LoadSearch.Click
        lb_SearchObjList.Items.Clear()
        _MySearchObjekte.Clear()

        Try
            Using SaveStreamReader As New StreamReader("SearchData.dat")
                Dim sZeile As String

                sZeile = SaveStreamReader.ReadLine

                'Continue to read until you reach the end of the file.
                Do While Not sZeile Is Nothing
                    Dim Strings() As String
                    Strings = sZeile.Split(CChar(";"))

                    Dim tmpSearchObj As New MySearchObj(Convert.ToInt32(Strings(0)), Strings(1), Convert.ToInt32(Strings(2)), Convert.ToInt32(Strings(3)), Convert.ToInt32(Strings(4)))
                    _MySearchObjekte.Add(tmpSearchObj)
                    sZeile = SaveStreamReader.ReadLine
                Loop
                'Close the file.
                SaveStreamReader.Close()
            End Using

            For Each obj In _MySearchObjekte
                lb_SearchObjList.Items.Add(obj.ToString())
            Next
        Catch ex As Exception

        End Try
    End Sub

    'TCP_Variablen
    Private Sub btn_TCP_Connect_Click(sender As Object, e As EventArgs) Handles btn_TCP_Connect.Click
        lbl_TCP_Status.ForeColor = Color.Black
        lbl_TCP_Status.Text = "Warten.."
        _TcpVariablen.Connect(tb_TCP_HOST.Text, CInt(num_TCP_Port.Value))
    End Sub
    Private Sub btn_TCPVariable_New_Click(sender As Object, e As EventArgs) Handles btn_TCPVariable_New.Click
        If tb_TCPVarible_Name.Text <> "" Then
            _TcpVariablen.AddVariable(tb_TCPVarible_Name.Text)
            _TcpVariablen.SetVariable(tb_TCPVarible_Name.Text, CInt(num_TCPVariable_Wert.Value))
            _refreshDataGridView()
        End If
    End Sub
    Private Sub btn_TCPVariable_Set_Click(sender As Object, e As EventArgs) Handles btn_TCPVariable_Set.Click
        If _TcpVariablen.Exists(tb_TCPVarible_Name.Text) Then
            _TcpVariablen.SetVariable(tb_TCPVarible_Name.Text, CInt(num_TCPVariable_Wert.Value))
        Else
            lb_Info.Items.Insert(0, $"Variablenfehler: Die Variable:{tb_TCPVarible_Name.Text}  Existiert nicht")
        End If
        _refreshDataGridView()
    End Sub
    Private Sub btn_TCPVariable_Del_Click(sender As Object, e As EventArgs) Handles btn_TCPVariable_Del.Click
        If _TcpVariablen.Exists(tb_TCPVarible_Name.Text) Then
            _TcpVariablen.RemoveVariable(tb_TCPVarible_Name.Text)
        Else
            lb_Info.Items.Insert(0, $"Variablenfehler: Die Variable:{tb_TCPVarible_Name.Text}  Existiert nicht")
        End If
        _refreshDataGridView()
    End Sub

    'Konfig
    Private Sub btn_tiefe_Click(sender As Object, e As EventArgs) Handles btn_tiefe.Click
        Dim MatResurce As New Mat
        If _MatDepth Is Nothing Then
            If _MatRefD IsNot Nothing Then
                MatResurce = _MatRefD
                Tiefentest(MatResurce, _MatColor, cb_Tiefe_aktMaske.Checked)
            Else
                lb_Info.Items.Insert(0, "Es existiert noch kein Tiefenbild, das Analysiert werden kann")
            End If
        Else
            MatResurce = _MatDepth
            Tiefentest(MatResurce, _MatColor, cb_Tiefe_aktMaske.Checked)
        End If
        Dim v1 As New ImageViewer : v1.Image = _MatColor.Clone : v1.Text = "Tiefe" : v1.Show()
        _MatColor.CopyTo(_DisColor)
        _MatDepthC.CopyTo(_DisDepthC)
        CvInvoke.Resize(_DisColor, _DisColor, New Size(640, 480))
        CvInvoke.Resize(_DisDepthC, _DisDepthC, New Size(640, 480))
        ib_res_02.Image = _DisColor.ToImage(Of Bgr, Byte)
        ib_res_01.Image = _DisDepthC.ToImage(Of Bgr, Byte)
        TC2_Bilder.SelectedTab = P2_Result
    End Sub

    Private Sub btn_pos_Click(sender As Object, e As EventArgs) Handles btn_pos.Click
        If _MatColor Is Nothing Then
            If _MatRefC IsNot Nothing Then
                _DisColor = Maskieren(_MatRefC, CInt(num_MaskH.Value), CInt(num_MaskV.Value), ColorCamOffset)
                _DisDepth = Maskieren(_MatRefDc, CInt(num_MaskH.Value), CInt(num_MaskV.Value), CInt(num_CamOffset.Value))
            Else
                lb_Info.Items.Insert(0, "Es existiert noch kein Bild, zum festlegen der Position")
            End If
        Else
            _DisColor = Maskieren(_MatColor, CInt(num_MaskH.Value), CInt(num_MaskV.Value), ColorCamOffset)
            _DisDepth = Maskieren(_MatDepthC, CInt(num_MaskH.Value), CInt(num_MaskV.Value), CInt(num_CamOffset.Value))
        End If
        'Anzeigen
        CvInvoke.Resize(_DisColor, _DisColor, New Size(640, 480))
        ib_res_01.Image = _DisColor.ToImage(Of Bgr, Byte)
        CvInvoke.Resize(_DisDepth, _DisDepth, New Size(640, 480))
        ib_res_02.Image = _DisDepth.ToImage(Of Bgr, Byte)
        TC2_Bilder.SelectedTab = P2_Result
    End Sub

    Private Sub btn_Konf_Col_Click(sender As Object, e As EventArgs) Handles btn_Konf_Col.Click
        'KonfigPipe(Enum_Format.color, cWidht, cHeight, Format.Bgr8, CInt(comb_Konf_Col_FPS.SelectedItem))
        lb_Info.Items.Insert(0, $"{comb_Konf_Col_FPS.SelectedItem};{comb_Konf_Col_Format.SelectedItem};{comb_Konf_Col_Auflösung.SelectedItem}")

    End Sub
    Private Sub btn_Konf_Depth_Click(sender As Object, e As EventArgs) Handles btn_Konf_Depth.Click
        'KonfigPipe(Enum_Format.depth, cWidht, cHeight, Format.Z16, CInt(comb_Konf_Dep_FPS.SelectedItem))
        lb_Info.Items.Insert(0, $"{comb_Konf_Dep_FPS.SelectedItem};{comb_Konf_Dep_Format.SelectedItem};{comb_Konf_Dep_Auflösung.SelectedItem}")
    End Sub

    Private Sub btn_CamOffset_Reset_Click(sender As Object, e As EventArgs) Handles btn_CamOffset_Reset.Click
        num_CamOffset.Value = cCamOffset
    End Sub

    Private Sub cb_colortaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub
    Private Sub cb_depthtaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub
    Private Sub cb_depthcTaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub
    Private Sub cb_refcTaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub
    Private Sub cb_refdTaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub
    Private Sub cb_refdcTaken_CheckedChanged(sender As Object, e As EventArgs)
        ImgStatus()
    End Sub

    'Konfig Farbe
    Private Sub comb_Konf_Col_FPS_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Col_FPS.SelectedIndexChanged

    End Sub
    Private Sub comb_Konf_Col_Format_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Col_Format.SelectedIndexChanged

    End Sub
    Private Sub comb_Konf_Col_Auflösung_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Col_Auflösung.SelectedIndexChanged

    End Sub
    'Konfig Tiefe
    Private Sub comb_Konf_Dep_FPS_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Dep_FPS.SelectedIndexChanged

    End Sub
    Private Sub comb_Konf_Dep_Format_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Dep_Format.SelectedIndexChanged

    End Sub
    Private Sub comb_Konf_Dep_Auflösung_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comb_Konf_Dep_Auflösung.SelectedIndexChanged

    End Sub

    'Referenzieren
    Private Sub btn_RefXY_Add_Click(sender As Object, e As EventArgs) Handles btn_RefXY_Add.Click
        If num_RefXY_KX.Value <> 0 Or num_RefXY_KY.Value <> 0 Or num_RefXY_RX.Value <> 0 Or num_RefXY_RY.Value <> 0 Then
            Dim tmp_Refobj As New MyRefObjekt(num_RefXY_RX.Value, num_RefXY_RY.Value, num_RefXY_KX.Value, num_RefXY_KY.Value)
            _MyRefXY_List.Add(tmp_Refobj)
        End If
    End Sub


    'Test
    Private Sub btn_TestVerschieben_Click(sender As Object, e As EventArgs) Handles btn_TestVerschieben.Click
        Dim test As New Mat
        Dim test2 As New Mat
        test = ImageVerschieben(_MatColor, CInt(num_CamOffset.Value))
        Dim v1, v2 As New ImageViewer
        v1.Image = test.Clone : v1.Text = "verschoben"
        v2.Image = _MatDepthC.Clone : v2.Text = " "
        CvInvoke.Imshow("Original", _MatColor)
        lb_Info.Items.Insert(0, $"test rows:{test.Rows} cols:{test.Cols} | depth rows:{_MatDepthC.Rows} cols:{_MatDepthC.Cols}")
        v1.Show()
        v2.Show()
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        CvInvoke.Circle(_MatColor, New Point(5, 5), 5, New MCvScalar(255, 0, 0), 2)
        CvInvoke.Circle(_MatColor, New Point(5, 5), 1, New MCvScalar(255, 0, 0), 1)
        Dim v1 As New ImageViewer : v1.Image = _MatColor.Clone : v1.Text = "1" : v1.Show()
        Dim v2 As New ImageViewer : v2.Image = _MatDepthC.Clone : v2.Text = "1" : v2.Show()
        'KantenFinden(_MatWatershedMask, _MatPoints)

        'Dim test As New MyObektV2(10, {255, 50, 255})
        'Dim Points(24) As Point
        'Dim z(24) As Int32
        'Dim höhe As Int16 = 10
        'Dim Anzahl As Int32 = 0
        'For i = 0 To 4
        '    For j = 0 To 4
        '        Points(Anzahl) = New Point(i, j)
        '        z(Anzahl) = höhe
        '        höhe += CShort(2)
        '        Anzahl += 1
        '    Next
        'Next
        'Dim testv As New VectorOfPoint
        'testv.Push(Points)
        'test.Add_Ref(testv, z)
        'Dim Objhöhe As Int32 = test.GetHöhe()
        'test.Add_Ref(5, 2, 100)
        'Objhöhe = test.GetHöhe()
        'Dim p As New MyPoint(2, 5, 0)
        'test.Add_Ref(p)
        'Dim Objbreite As Int32 = test.GetBreite()
        'Dim testmatc As New Mat
        'testmatc = Mat.Zeros(640, 480, DepthType.Cv8U, 3)
        'Dim testmato As New Mat
        'testmato = Mat.Zeros(640, 480, DepthType.Cv8U, 3)
        'Dim testv2 As New VectorOfVectorOfPoint()

        'CvInvoke.DrawContours(testmatc, test.GetContours, 0, New MCvScalar(test.Color(0), test.Color(0), test.Color(0)), 2)
        'CvInvoke.Imshow("Kontur", testmatc)
        'CvInvoke.DrawContours(testmato, test.GetContours, 0, New MCvScalar(test.Color(0), test.Color(0), test.Color(0)), -1)
        'CvInvoke.Imshow("Objekt", testmato)
        'Dim pout As Point()
        'pout = test.GetOuterPoints()

        'Dim pin As Point()
        'pin = test.GetMinAreaPoints()

        ''draw Boxes
        'For i = 0 To 3
        '    CvInvoke.Line(testmato, pout(i), pout((i + 1) Mod 4), New MCvScalar(0, 255, 0), 2) 'out
        '    CvInvoke.Line(testmato, pin(i), pin((i + 1) Mod 4), New MCvScalar(0, 0, 255), 2) 'min
        'Next
        'CvInvoke.PutText(testmato, $"ID:{test.ID,2} Winkel:{test.GetWinkel.ToString("N2"),4}", test.GetZentrumPoint, FontFace.HersheyComplex, 1, New MCvScalar(255, 255, 255))
        'CvInvoke.Imshow("t", testmato)
    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Einstelungen
    '-----------------------------------------------------------------------------------------------------------------------
    Private Sub num_CamOffset_ValueChanged(sender As Object, e As EventArgs) Handles num_CamOffset.ValueChanged
        ColorCamOffset = CInt(num_CamOffset.Value) - cCamOffset
    End Sub

    Private Sub LoadSettings()
        'Steuerung
        Num_SearchToleranz.Value = My.Settings.Steur_Tolreanz
        'Konfiguration
        num_MaskH.Value = My.Settings.Konf_MaskeH
        num_MaskV.Value = My.Settings.Konf_MaskeV
        num_CamOffset.Value = My.Settings.Konf_KameraOffset
        ColorCamOffset = My.Settings.Konf_KameraColorOffset
        num_pixmmH_faktor.Value = CDec(My.Settings.Konv_FaktorPixMilH)
        num_pixmmB_faktor.Value = CDec(My.Settings.Konv_FaktorPixMilB)
        num_RoboOffsetX.Value = CDec(My.Settings.Konv_RoboOffsetX)
        num_RoboOffsety.Value = CDec(My.Settings.Konv_RoboOffsetY)
        'Auswertung
        num_ThreshTief.Value = My.Settings.Ausw_TiefsteReg
        num_ThreshHoch.Value = My.Settings.Ausw_HöchsteReg
        num_WTS_MinH.Value = My.Settings.Ausw_MindestObjH
        num_WTS_MinB.Value = My.Settings.Ausw_MindestObjB
        num_WTS_MinT.Value = My.Settings.Ausw_MindestObjT
        'TCP
        tb_TCP_HOST.Text = My.Settings.TCP_Host
        num_TCP_Port.Value = My.Settings.TCP_Port
    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Timer
    '-----------------------------------------------------------------------------------------------------------------------
    Private Sub T2_CamBoot_Tick(sender As Object, e As EventArgs) Handles T2_CamBoot.Tick
        T2_CamBoot.Stop()
        'If (TakePicture(_MatRefC, _MatRefD, _MatRefDc)) Then
        '    lb_Info.Items.Insert(0, "Referenzbilder Color erfolgreich")
        '    _MatRefC.CopyTo(_DisRefC)
        '    CvInvoke.Resize(_DisRefC, _DisRefC, New Size(640, 480))
        '    ib_Color.Image = _DisRefC.ToImage(Of Bgr, Byte)
        '    _MatRefDc.CopyTo(_DisRefD)
        '    CvInvoke.Resize(_DisRefD, _DisRefD, New Size(640, 480))
        '    ib_Depth.Image = _DisRefD.ToImage(Of Bgr, Byte)
        'End If
    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Events
    '-----------------------------------------------------------------------------------------------------------------------
    'TCP_Variablen
    Private Sub _TcpVariablen_Connected() Handles _TcpVariablen.Connected
        lbl_TCP_Status.ForeColor = Color.Green
        lbl_TCP_Status.Text = "Verbunden"
    End Sub
    Private Sub _TcpVariablen_ConnectError() Handles _TcpVariablen.ConnectError
        lbl_TCP_Status.ForeColor = Color.Red
        lbl_TCP_Status.Text = "Fehler"
    End Sub
    Private Sub _TcpVariablen_Disconnected() Handles _TcpVariablen.Disconnected
        Me.Invoke(Sub() Disconect())
    End Sub

    Private Sub _TcpVariablen_VariableChanged(name As String, val As Double) Handles _TcpVariablen.VariableChanged
        Me.Invoke(Sub() VaribleChange(name, val))

    End Sub

    Sub Disconect()
        lbl_TCP_Status.ForeColor = Color.Yellow
        lbl_TCP_Status.Text = "Getrennt"
    End Sub

    Sub VaribleChange(name As String, val As Double)
        If name = "pic" And val > 0 Then
            _TcpVariablen.SetVariable("pic", 0)
            Dim rdy As Double = Auto()
            _TcpVariablen.SetVariable("rdy", rdy)
        Else
            _refreshDataGridView()
            lb_Info.Items.Insert(0, $"Variabel wurde geändert {name} {val}")
        End If
    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Sonstige Sub's
    '-----------------------------------------------------------------------------------------------------------------------
    Private Sub KonfigPipe(typ As Enum_Format, widht As Int32, height As Int32, format As Format, fps As Int32)

        _MyCFG.DisableAllStreams()
        Try
            'Color Konfig
            If typ = Enum_Format.color Then
                _MyCFG.EnableStream(Intel.RealSense.Stream.Color, widht, height, format, fps)
                lb_Info.Items.Insert(0, "RGB_Konfig added")
            End If

            'Depth Konfig
            If typ = Enum_Format.depth Then
                _MyCFG.EnableStream(Intel.RealSense.Stream.Depth, widht, height, format, fps)
                lb_Info.Items.Insert(0, "Depth_Konfig added")
            End If

            RestartCam()
        Catch ex As Exception
            lb_Info.Items.Insert(0, $"Konfigurations oder Pipeline Fehle: {ex.Message}")
            Return
        End Try
    End Sub
    Private Sub KonfigPipe()

        _MyCFG.DisableAllStreams()
        Try
            'Color Konfig

            _MyCFG.EnableStream(Intel.RealSense.Stream.Color, cWidht, cHeight, Format.Bgr8, cfpsColor)
            lb_Info.Items.Insert(0, "RGB_Konfig added")
            'Depth Konfig
            _MyCFG.EnableStream(Intel.RealSense.Stream.Depth, cWidht, cHeight, Format.Z16, cfpsDepth)
            lb_Info.Items.Insert(0, "Depth_Konfig added")

            RestartCam()
        Catch ex As Exception
            lb_Info.Items.Insert(0, $"Konfigurations oder Pipeline Fehle: {ex.Message}")
            Return
        End Try
    End Sub

    Private Sub RestartCam()
        If _MyPipelineAktiv Then
            _MyPipelineAktiv = False
            _MyPipeline.Stop()
            '_MyPipeline.Release()
            '_MyPipeline.Dispose()
        End If
        btn_NewImg.Enabled = False
        btn_RefImg.Enabled = False
        btn_Analyse.Enabled = False
        btn_SearchObj.Enabled = False
        Thread.Sleep(50)
        Try
            _MyPipeline.Start(_MyCFG)
            _MyPipelineAktiv = True
            T2_CamBoot.Start()
            lb_Info.Items.Insert(0, $"Cam_Boot")
        Catch ex As Exception
            lb_Info.Items.Insert(0, $"Konfigurations oder Pipeline Fehle: {ex.Message}")
            _MyPipelineAktiv = False
        End Try
        Thread.Sleep(50)
        If _MyPipelineAktiv Then
            btn_NewImg.Enabled = True
            btn_RefImg.Enabled = True
            btn_Analyse.Enabled = True
            btn_SearchObj.Enabled = True
            lb_Info.Items.Insert(0, $"Cam_Ready")
        End If

    End Sub

    Private Sub ImgStatus()
        If (_MatColor IsNot Nothing) Then
            _ColorImgTaken = (Not _MatColor.IsEmpty)
        End If
        If (_MatDepth IsNot Nothing) Then
            _DepthImgTaken = (Not _MatDepth.IsEmpty)
        End If
        If (_MatDepthC IsNot Nothing) Then
            _DepthCImgTaken = (Not _MatDepthC.IsEmpty)
        End If
        If (_MatRefC IsNot Nothing) Then
            _RefCImgTaken = (Not _MatRefC.IsEmpty)
        End If
        If (_MatRefD IsNot Nothing) Then
            _RefDImgTaken = (Not _MatRefD.IsEmpty)
        End If
        cb_colortaken.Checked = _ColorImgTaken
        cb_depthtaken.Checked = _DepthImgTaken
        cb_depthcTaken.Checked = _DepthCImgTaken
        cb_refcTaken.Checked = _RefCImgTaken
        cb_refdcTaken.Checked = _RefDImgTaken
        cb_refdTaken.Checked = _RefDImgTaken

        btn_tiefe.Enabled = _DepthImgTaken Or _RefDImgTaken
        btn_pos.Enabled = _ColorImgTaken Or _RefCImgTaken
    End Sub

    Private Sub Autoreferenz()
        'For i = 1 To 10  ' Möglicherweise machen hier 30 Frames Sinn
        '    Using frames = _MyPipeline.WaitForFrames()
        '    End Using
        'Next
        T2_CamBoot.Start()
    End Sub

    Private Sub ImgOfInterest_Tiefe(resurce As Mat, result As Mat, MinDepth As Double, MaxDepth As Double)
        Dim dst As New Mat
        CvInvoke.Threshold(resurce, dst, MinDepth, 100, ThresholdType.ToZero)
        CvInvoke.Threshold(dst, result, MaxDepth, 100, ThresholdType.ToZeroInv)
    End Sub

    Private Sub ImgOfInterest_Rev(resurce As Mat, result As Mat, rev As Mat)
        CvInvoke.AbsDiff(resurce, rev, result)
    End Sub

    Private Sub Tiefentest(resurce As Mat, result As Mat, mask As Boolean)
        Dim PointMin As MyPoint
        Dim PointMax As MyPoint
        Dim wert As Int16
        Dim StartZeile, EndZeile, StartSpalte, Endspalte As Int32
        If mask Then
            StartSpalte = CInt(num_MaskV.Value - num_CamOffset.Value)
            Endspalte = CInt(resurce.Cols - (num_MaskV.Value - num_CamOffset.Value) + 1)
            StartZeile = CInt(num_MaskH.Value)
            EndZeile = CInt(resurce.Rows - (num_MaskH.Value + 1))
        Else
            StartZeile = 0
            EndZeile = resurce.Rows - 1
            StartSpalte = 0
            Endspalte = resurce.Cols - 1
        End If
        wert = UmwandlungClass.GetInt16Value(resurce, 0, 0)
        PointMax = New MyPoint(0, 0, 0)
        PointMin = New MyPoint(0, 0, Int16.MaxValue)
        For Zeile = StartZeile To EndZeile
            For Spalte = StartSpalte To Endspalte
                Try
                    wert = UmwandlungClass.GetInt16Value(resurce, Zeile, Spalte)
                    If wert <> 0 Then
                        If PointMax.Z < wert Then
                            PointMax.X = Spalte
                            PointMax.Y = Zeile
                            PointMax.Z = wert
                        End If
                        If PointMin.Z > wert Then
                            PointMin.X = Spalte
                            PointMin.Y = Zeile
                            PointMin.Z = wert
                        End If
                    End If
                Catch ex As Exception
                    lb_Info.Items.Insert(0, $"Wert bei x:{Spalte,4} y:{Zeile,4} nicht gültig")
                End Try

            Next
        Next
        lbl_pointMin.ForeColor = Color.Blue
        lbl_pointMin.Text = PointMax.ToString
        lbl_pointMax.ForeColor = Color.Green
        lbl_pointMax.Text = PointMin.ToString
        CvInvoke.Circle(result, New Point(PointMin.X - ColorCamOffset, PointMin.Y), 12, New MCvScalar(127, 255, 127), 2)
        CvInvoke.Circle(result, New Point(PointMin.X - ColorCamOffset, PointMin.Y), 8, New MCvScalar(127, 255, 127), 3)
        CvInvoke.Circle(result, New Point(PointMin.X - ColorCamOffset, PointMin.Y), 2, New MCvScalar(0, 255, 0), 1)
        CvInvoke.Circle(result, New Point(PointMax.X - ColorCamOffset, PointMax.Y), 12, New MCvScalar(255, 127, 127), 2)
        CvInvoke.Circle(result, New Point(PointMax.X - ColorCamOffset, PointMax.Y), 8, New MCvScalar(255, 127, 127), 3)
        CvInvoke.Circle(result, New Point(PointMax.X - ColorCamOffset, PointMax.Y), 2, New MCvScalar(255, 0, 0), 1)
    End Sub

    Private Sub _refreshDataGridView()
        dgv_TCPVariableViewer.DataSource = _TcpVariablen.GetGridViewDataSource
    End Sub

    Private Sub ZeichneObjekte2(ByRef mask As Mat)
        Dim tmp_mask As New Mat
        tmp_mask = mask.Clone
        Dim ZeichenMat3 = Mat.Zeros(tmp_mask.Rows, tmp_mask.Cols, DepthType.Cv8U, 3)
        For Zeile = 0 To mask.Rows - 1

            For Spalte = 0 To mask.Cols - 1
                Dim Marke As Int32 = UmwandlungClass.GetInt32Value(mask, Zeile, Spalte)
                For Each obj In _MyMatchObjekts
                    If Marke = obj.Objekt.ID Then
                        Dim color As Byte() = {obj.Objekt.Color(0), obj.Objekt.Color(1), obj.Objekt.Color(2)}
                        If color IsNot Nothing Then
                            UmwandlungClass.SetByteValues(ZeichenMat3, Zeile, Spalte, color)
                        Else
                            color = {255, 255, 255}
                            UmwandlungClass.SetByteValues(ZeichenMat3, Zeile, Spalte, color)
                        End If
                    End If
                Next
            Next
            Dim z As Int32 = Zeile
        Next
        _MatFound = ZeichenMat3.Clone
        CvInvoke.Resize(_MatFound, _DisFound, New Size(640, 480))
        ib_Found.Image = _DisFound.Clone
    End Sub

    '-----------------------------------------------------------------------------------------------------------------------
    'Sonstige Funktionen
    '-----------------------------------------------------------------------------------------------------------------------

    Private Function PixToMil(pixel As Int32) As Int32
        Return CInt(Math.Round(pixel / ((num_pixmmB_faktor.Value + num_pixmmH_faktor.Value) / 2)))
    End Function
    Private Function MilToPix(milimeter As Int32) As Int32
        Return CInt(Math.Round(milimeter * ((num_pixmmB_faktor.Value + num_pixmmH_faktor.Value) / 2)))
    End Function

    Private Function TakePicture(ByRef color As Mat, ByRef depth As Mat, ByRef depthc As Mat) As Boolean
        Dim Mydata As FrameSet
        lb_Info.Items.Insert(0, $"---------------------------------")
        lb_Info.Items.Insert(0, $"TakePicture: Neues Bild")
        'Bildvorhanden zurücksetzen
        If _MyPipeline.PollForFrames(Mydata) Then
            If Mydata IsNot Nothing Then
                color = ToMat(Mydata.ColorFrame).Clone
                depth = ToMat(Mydata.DepthFrame, DepthType.Cv16S).Clone
                depthc = ToMat(_MyColorMap.Colorize(Mydata.DepthFrame)).Clone
            Else
                lb_Info.Items.Insert(0, "TakePicture: Fehler PollForFrames gab keine Daten zurück")
            End If
        Else
            lb_Info.Items.Insert(0, $"TakePicture: Fehler bei PollForFrames [{_trys,2}]")
            If _trys < cMaxTrys Then
                _trys += 1
                Return False
            Else
                _trys = 0
                lb_Info.Items.Insert(0, "TakePicture: Auto Restart")
                RestartCam()
                Return False
            End If
        End If
        ImgStatus()
        Return True
    End Function

    Private Function ToMat(Source As VideoFrame, Optional Depth As DepthType = DepthType.Cv8U) As Mat
        Dim NrChanels As Int32
        If Depth = DepthType.Cv16S Or Depth = DepthType.Cv16U Then
            NrChanels = Source.BitsPerPixel \ 16
        ElseIf Depth = DepthType.Cv8S Or Depth = DepthType.Cv8U Then
            NrChanels = Source.BitsPerPixel \ 8
        Else
            NrChanels = Source.BitsPerPixel \ 8
        End If
        Return New Mat(New Size(cWidht, cHeight), Depth, NrChanels, Source.Data, Source.Stride)
    End Function

    Private Function ImgageAnalyse() As Boolean
        'Alte Objekte löschen
        LB_obj.Items.Clear()
        _MyObjekte.Clear()
        Dim vortsetzen As Boolean = False
        Dim Ergebnis As Boolean = False
        If Not _ColorImgTaken Or Not _DepthImgTaken Then
            vortsetzen = TakePicture(_MatColor, _MatDepth, _MatDepthC)
            If vortsetzen Then
                CvInvoke.Resize(_MatColor, _DisColor, New Size(640, 480))
                ib_new_Color.Image = _DisColor
                CvInvoke.Resize(_MatDepthC, _DisDepthC, New Size(640, 480))
                ib_new_Depth.Image = _DisDepthC
            End If
        Else
            vortsetzen = True
        End If

        If vortsetzen Then
            If (cb_ioi_Differenz.Checked And _RefCImgTaken And _RefDImgTaken) Or Not cb_ioi_Differenz.Checked Then
                If (cb_ioi_Depth.Checked And _DepthImgTaken) Or Not cb_ioi_Depth.Checked Then
                    If rb_Auswertung_Color.Checked Then
                        Ergebnis = MyWatershedColor(_MatColor, _MatResult, _MatRefC, cb_debug.Checked)
                    End If
                    If rb_Auswertung_Depth.Checked Or rb_Auswertung_Kombi.Checked Then
                        Ergebnis = MyWatershedDepthAndKombi(_MatColor, _MatDepth, _MatResult, _MatRefD, cb_debug.Checked)
                    End If
                Else
                    lb_Info.Items.Insert(0, "Für Tiefen Filderung wird ein Tiefenbild benötigt")
                    Return False
                End If
            Else
                lb_Info.Items.Insert(0, "Für Differenz Erkennung wird ein Reverenzbild benötigt")
                Return False
            End If
            If Ergebnis Then
                CvInvoke.Resize(_MatResult, _DisRes01, New Size(640, 480))
                ib_res_01.Image = _DisRes01.ToImage(Of Bgr, Byte)
                'CvInvoke.Resize(_DisDepth, _DisDepth, New Size(640, 480))
                'ib_Depth.Image = _DisDepth.ToImage(Of Bgr, Byte)
                'ib_Color.Image = _MatDepth.ToImage(Of Gray, Byte)
                'ib_Depth.Image = _MatDif.ToImage(Of Bgr, Byte)
                TC2_Bilder.SelectedTab = P2_Result
            End If
            Return Ergebnis
        Else
            lb_Info.Items.Insert(0, "Es wurden keine Bilder gefunden")
            Return False
        End If
    End Function

    Private Function Maskieren(resurce As Mat, maskH As Int32, maskV As Int32, Optional offset As Int32 = 0) As Mat
        Dim Maske As Mat
        Dim Ergebnis As New Mat
        Dim MIn As Byte() = {CByte(1), CByte(1), CByte(1)}
        Dim MOut As Byte() = {CByte(0), CByte(0), CByte(0)}
        If resurce.NumberOfChannels = 3 Then
            Maske = New Mat(New Size(resurce.Width, resurce.Height), DepthType.Cv8U, 3)

        Else
            Maske = New Mat(New Size(cWidht, cHeight), DepthType.Cv16S, 1)

        End If
        Dim UperBord As Int32 = maskH '135
        Dim LowerBord As Int32 = Maske.Height - maskH '720-135 = 585
        Dim LeftBord As Int32 = maskV - offset '305
        Dim RightBord As Int32 = Maske.Width - maskV - offset '1280-305 = 975
        lb_Info.Items.Insert(0, $"Lower Bord: {LowerBord} | Right Bord: {RightBord}")
        For Zeile = 0 To Maske.Rows
            For Spalte = 0 To Maske.Cols
                'If (Zeile <= LeftBord Or Zeile >= RightBord) And (Spalte <= UperBord Or Spalte >= LowerBord) Then
                If (Zeile > UperBord And Zeile < LowerBord) And (Spalte > LeftBord And Spalte < RightBord) Then
                    UmwandlungClass.SetByteValues(Maske, Zeile, Spalte, MIn)
                Else
                    UmwandlungClass.SetByteValues(Maske, Zeile, Spalte, MOut)
                End If
            Next
        Next
        TC2_Bilder.TabPages(5).Text = "Maske"
        ib_depth_01.Image = Maske
        CvInvoke.Multiply(resurce, Maske, Ergebnis)
        Return Ergebnis
    End Function

    Private Function ImageVerschieben(resurce As Mat, offset As Int32) As Mat
        Dim Verschoben As New Mat(resurce.Size, DepthType.Cv8U, 3)
        Verschoben.SetTo(New MCvScalar(0)) ' AnzeigenBild auf schwarz setzen
        For Zeile = 0 To Verschoben.Rows - 1
            For Spalte = 0 To Verschoben.Cols - 1
                If Spalte <= (Verschoben.Cols - (offset + 2)) Then
                    Dim value As Byte() = UmwandlungClass.GetByteValue(resurce, Zeile, Spalte + offset)
                    UmwandlungClass.SetByteValues(Verschoben, Zeile, Spalte, value)
                End If
            Next
        Next
        Return Verschoben
    End Function

    Private Function MyWatershedColor(resurce As Mat, result As Mat, rev As Mat, Optional debug As Boolean = False) As Boolean
        Dim ImgOfInterest As New Mat
        Dim Laplace32F As Mat
        Dim Result32F As Mat
        Dim IMG32F As Mat
        Dim Laplace8U As Mat
        Dim Result8U As Mat
        Dim GrayImg As New Mat
        Dim BinaerImg As New Mat
        Dim DistImg As New Mat
        Dim DistImgClear As New Mat
        Dim DistImg8u As New Mat
        Dim Markers As New Mat
        Dim Mask As New Mat
        Dim TmpResult As New Mat
        Dim dst As New Mat

        Const cThreshhold1 = 10
        Const cThreshhold2 = 0.3


        '1. Hintergrung Ausblenden entweder Mit Referenzbild oder über Tiefe
        ' Über Tiefe und Treshold
        'ImgOfInterest_Tiefe(resurce, ImgOfInterest, 0.25, 1.25) 'Tiefe zwischen 25cm und 1.25m
        'ImgOfInterest_Tiefe(resurce, ImgOfInterest, 5600, 7300) 'Tiefe zwischen 29cm und 69cm
        ' Über Reverenzbild
        ImgOfInterest_Rev(resurce, ImgOfInterest, rev)
        '########
        'CvInvoke.CvtColor(ImgOfInterest, ImgOfInterest, ColorConversion.Bgr2Gray, 1)
        'CvInvoke.Threshold(ImgOfInterest, ImgOfInterest, 100, 255, ThresholdType.ToZero)
        ''CvInvoke.GaussianBlur(picture, gausPic, New Drawing.Size(3, 3), 2)
        'CvInvoke.Erode(ImgOfInterest, ImgOfInterest, Nothing, New Point(-1, -1), 3, BorderType.Default, New MCvScalar(1))
        'CvInvoke.Dilate(ImgOfInterest, ImgOfInterest, Nothing, New Point(-1, -1), 3, BorderType.Default, New MCvScalar(1))

        If debug Then
            CvInvoke.Imshow("ImgOfInterest", ImgOfInterest)
        End If

        '2. Bild Aufbereiten (laplace)
        Dim Kernel As New Matrix(Of Single)(New Single(,) {
                                            {1, 1, 1},
                                            {1, -8, 1},
                                            {1, 1, 1}})
        Laplace32F = New Mat(ImgOfInterest.Size, DepthType.Cv32F, ImgOfInterest.NumberOfChannels)
        Result32F = New Mat(ImgOfInterest.Size, DepthType.Cv32F, ImgOfInterest.NumberOfChannels)
        CvInvoke.Filter2D(ImgOfInterest, Laplace32F, Kernel, New Point(-1, -1))
        IMG32F = New Mat : ImgOfInterest.ConvertTo(IMG32F, DepthType.Cv32F)
        CvInvoke.Subtract(IMG32F, Laplace32F, Result32F)
        'zurückwandeln
        Laplace8U = New Mat
        Result8U = New Mat
        Laplace32F.ConvertTo(Laplace8U, DepthType.Cv8U)
        Result32F.ConvertTo(Result8U, DepthType.Cv8U)
        If debug Then
            CvInvoke.Imshow("Result32", Result32F)
            CvInvoke.Imshow("Result8", Result8U)
        End If
        CvInvoke.GaussianBlur(Result8U, Result8U, New Drawing.Size(5, 5), 3)

        '3. Graustufen- & Binäresbild erzeugen
        If Result8U.NumberOfChannels = 3 Then
            CvInvoke.CvtColor(Result8U, GrayImg, ColorConversion.Bgr2Gray, 1)
        Else
            Result8U.CopyTo(GrayImg)
        End If
        CvInvoke.Threshold(GrayImg, BinaerImg, cThreshhold1, 255, ThresholdType.Binary Or ThresholdType.Otsu)
        If debug Then
            CvInvoke.Imshow("BinaerImg", BinaerImg)
        End If



        '4. Distanc Funktion & normalize
        CvInvoke.DistanceTransform(BinaerImg, DistImg, Nothing, DistType.L2, 3)
        CvInvoke.Normalize(DistImg, DistImg, 0, 1.0, NormType.MinMax)
        If debug Then
            CvInvoke.Imshow("DistImg", DistImg)
        End If

        '5. Bild bereinigen (mit Treshold) und in CV8 wandeln
        CvInvoke.Threshold(DistImg, DistImgClear, cThreshhold2, 1.0, ThresholdType.Binary)

        Dim kernel1 = Mat.Ones(3, 3, DepthType.Cv8U, 1)
        CvInvoke.Dilate(DistImgClear, DistImgClear, kernel1, New Point(-1, -1), 1, BorderType.Default, New MCvScalar(0))

        If debug Then
            CvInvoke.Imshow("DistImgClear", DistImgClear)
        End If

        CvInvoke.Normalize(DistImgClear, DistImg8u, 0, 255, NormType.MinMax, DepthType.Cv8U)

        '6. Objekte bezeichnen
        Dim Contours As New VectorOfVectorOfPoint
        CvInvoke.FindContours(DistImg8u, Contours, Nothing, RetrType.List, ChainApproxMethod.ChainApproxSimple)
        lb_Info.Items.Insert(0, $"Es wurden {Contours.Size} Kontuen Gefunden")
        ' Create the marker image for the watershed algorithm
        Markers = New Mat(resurce.Size, DepthType.Cv32S, 1) ' Zur Anzeige farbiger Bilder
        Markers.SetTo(New MCvScalar(0)) ' AnzeigenBild auf schwarz setzen
        For i = 0 To Contours.Size - 1
            Dim Colo = New MCvScalar((i + 1) * 10) ' An 1 nummerieren. 0 ist ja der Hintergrund
            CvInvoke.DrawContours(Markers, Contours, i, Colo, -1) ' -1 damit die contour ausgefüllt wird
        Next
        If debug Then
            Dim Maskshow As New Mat
            Markers.ConvertTo(Maskshow, DepthType.Cv8U)
            CvInvoke.Imshow("markers32S1", Maskshow)
        End If

        CvInvoke.Circle(Markers, New Point(5, 5), 3, New MCvScalar(255), -1)
        If debug Then
            Dim Maskshow2 As New Mat
            Markers.ConvertTo(Maskshow2, DepthType.Cv8U)
            CvInvoke.Imshow("markers32S1_2", Maskshow2)
        End If

        '7. Watershed zu objekt Auswertung  Unklar ob Result8U oder DistImg8u
        Dim imgWater8U3 As New Mat
        If Result8U.NumberOfChannels = 3 Then
            imgWater8U3 = Result8U
        Else
            CvInvoke.CvtColor(Result8U, imgWater8U3, ColorConversion.Gray2Bgr, 3)
        End If
        ' CvInvoke.WaitKey(0)
        CvInvoke.Watershed(imgWater8U3, Markers)

        '8. Farbenvergeben
        Dim Nc = Contours.Size ' Anzahl Contours = Anzahl an markierten Elementen
        Dim Cols(Nc) As MCvScalar : Dim RND As New Random
        For i = 0 To Nc '- 1
            Cols(i) = New Bgr(RND.Next(0, 256), RND.Next(0, 256), RND.Next(0, 256)).MCvScalar
        Next

        Dim ZeichenMat2 = Mat.Zeros(Markers.Rows, Markers.Cols, DepthType.Cv8U, 3)
        For Zeile = 0 To Markers.Rows - 1
            For Spalte = 0 To Markers.Cols - 1
                Dim wert As Int32
                Dim gefunden As Boolean = False
                wert = UmwandlungClass.GetInt32Value(Markers, Zeile, Spalte)
                If _MyObjekte.Count < 1 And wert > 0 Then
                    Dim tmpObjekt As New MyObjektV2(wert)
                    Dim tiefe As Int32 = UmwandlungClass.GetInt32Value(_MatDepth, Zeile, Spalte)
                    tmpObjekt.Add_Ref(Spalte, Zeile, tiefe)
                    _MyObjekte.Add(tmpObjekt)
                Else
                    For Each ob As MyObjektV2 In _MyObjekte
                        If wert = ob.ID Then
                            gefunden = True
                            Dim tiefe As Int32 = UmwandlungClass.GetInt32Value(_MatDepth, Zeile, Spalte)
                            ob.Add_Ref(Spalte, Zeile, tiefe)
                        End If
                    Next
                    If Not gefunden And wert > 0 Then
                        Dim tmpObjekt As New MyObjektV2(wert)
                        Dim tiefe As Int32 = UmwandlungClass.GetInt32Value(_MatDepth, Zeile, Spalte)
                        tmpObjekt.Add_Ref(Spalte, Zeile, tiefe)
                        _MyObjekte.Add(tmpObjekt)
                    End If
                End If
                Dim Marke As Int32 = UmwandlungClass.GetInt32Value(Markers, Zeile, Spalte)
                If Marke > 0 And Marke <= Nc * 10 Then
                    Dim ByteWerte() As Byte = {CByte(Cols(Marke \ 10).V0), CByte(Cols(Marke \ 10).V1), CByte(Cols(Marke \ 10).V2)}
                    UmwandlungClass.SetByteValues(ZeichenMat2, Zeile, Spalte, ByteWerte)
                End If
            Next
        Next
        For Each ob2 As MyObjektV2 In _MyObjekte
            LB_obj.Items.Add(ob2.ToString)
            'Zentrum
            CvInvoke.Circle(ZeichenMat2, ob2.GetZentrumPoint, 5, New MCvScalar(255, 255, 255), 2)
            CvInvoke.Circle(ZeichenMat2, ob2.GetZentrumPoint, 1, New MCvScalar(255, 0, 255), 1)

            Dim pin As Point()
            pin = ob2.GetMinAreaPoints()
            '0-Punkt
            CvInvoke.Circle(ZeichenMat2, pin(0), 3, New MCvScalar(255, 255, 0), 2)
            'draw Boxes
            For i = 0 To 2
                CvInvoke.Line(ZeichenMat2, pin(i), pin((i + 1)), New MCvScalar(0, 0, 255), 2) 'min
            Next
            CvInvoke.Line(ZeichenMat2, pin(3), pin(0), New MCvScalar(255, 255, 255), 2) 'Grundlinie
            CvInvoke.PutText(ZeichenMat2, $"ID:{ob2.ID,2}", ob2.GetZentrumPoint, FontFace.HersheyComplex, 1, New MCvScalar(255, 255, 255), 2)


        Next
        ZeichenMat2.CopyTo(result)
        If debug Then
            CvInvoke.Imshow("Ergebnis", ZeichenMat2)
        End If

        Return True
    End Function

    Private Function MyWatershedDepthAndKombi(ByRef resurceColor As Mat, ByRef resurceDepth As Mat, ByRef result As Mat, ByRef rev As Mat, Optional debug As Boolean = False) As Boolean
        Dim Display1 As New Mat
        Dim Display2 As New Mat

        Dim Konturen As New VectorOfVectorOfPoint

        Dim tmp_Verschoben As New Mat
        Dim tmp_IOIColor As New Mat
        Dim tmp_IOIDepth As New Mat
        Dim tmp_LaplaceColor As New Mat
        Dim tmp_LaplaceDepth As New Mat
        Dim tmp_Binaer As New Mat
        Dim tmp_DistObj As New Mat
        Dim tmp_DistBack As New Mat
        Dim tmp_Maske As New Mat
        Dim tmp_Watershed As New Mat
        Dim tmp_Points As New Mat


        Const cThreshhold1 = 10
        Const cThreshhold2 = 0.1 '0.3
        Const cThreshhold3 = 0.2 '0.3
        Dim cDisplaySize As New Size(640, 480)

        '0. Farbbild verschieben damit Fabe und Tiefe Dekungsgleich sind
        tmp_Verschoben = ImageVerschieben(resurceColor, CInt(num_CamOffset.Value))

        '1. Hintergrung Ausblenden über Tiefe dann Kiste Ausmaskieren
        'Depth
        If Not WM_ImageOfInterest(resurceDepth, tmp_IOIDepth, rev, cb_ioi_Differenz.Checked, cb_ioi_Mask.Checked, cb_ioi_Depth.Checked, cb_ioi_Gaus.Checked, CInt(num_CamOffset.Value)) Then
            lb_Info.Items.Insert(0, "Fehler bei: WM_ImageOfInterest")
            Return False
        End If
        'Color
        If Not WM_ImageOfInterest(tmp_Verschoben, tmp_IOIColor,,, True,, True, CInt(num_CamOffset.Value)) Then
            lb_Info.Items.Insert(0, "Fehler bei: WM_ImageOfInterest")
            Return False
        End If
        CvInvoke.Resize(tmp_IOIColor, Display1, cDisplaySize)
        CvInvoke.Resize(tmp_IOIDepth, Display2, cDisplaySize)
        Display2.ConvertTo(Display2, DepthType.Cv8U)
        ib_ImOfIn_01.Image = Display1.Clone
        ib_ImOfIn_02.Image = Display2.Clone

        '2. Bild Aufbereiten (laplace)
        'Depth
        If Not WM_LaplaceFiltering(tmp_IOIDepth, tmp_LaplaceDepth) Then
            lb_Info.Items.Insert(0, "Fehler bei: WM_LaplaceFiltering")
            Return False
        End If
        'Color
        If Not WM_LaplaceFiltering(tmp_IOIColor, tmp_LaplaceColor) Then
            lb_Info.Items.Insert(0, "Fehler bei: WM_LaplaceFiltering")
            Return False
        End If
        CvInvoke.Resize(tmp_LaplaceColor, Display1, cDisplaySize)
        CvInvoke.Resize(tmp_LaplaceDepth, Display2, cDisplaySize)
        Display2.ConvertTo(Display2, DepthType.Cv8U)
        ib_laplace_01.Image = Display1.Clone
        ib_laplace_02.Image = Display2.Clone

        '3. Graustufen- & Binäresbild erzeugen
        If Not WM_Graustufen_Binärbild(tmp_LaplaceDepth, tmp_Binaer, cThreshhold1) Then
            lb_Info.Items.Insert(0, "Fehler bei: WM_Graustufen_Binärbild")
            Return False
        End If

        '4. Distanc Funktion & normalize
        If Not WM_DistanceDetection(tmp_Binaer, tmp_DistObj, tmp_DistBack, cThreshhold2, cThreshhold3) Then
            lb_Info.Items.Insert(0, "Fehler bei: WM_DistanceDetection")
            Return False
        End If
        CvInvoke.Resize(tmp_DistObj, Display1, cDisplaySize)
        CvInvoke.Resize(tmp_DistBack, Display2, cDisplaySize)
        ib_Dist01.Image = Display1.Clone
        ib_Dist02.Image = Display2.Clone

        '5. Objekte bezeichnen
        If Not WM_MarkObjects(tmp_DistObj, tmp_DistBack, tmp_Maske, Konturen) Then
            lb_Info.Items.Insert(0, "Fehler bei: WM_MarkObjects")
            Return False
        End If
        tmp_Maske.ConvertTo(Display2, DepthType.Cv8U)
        CvInvoke.Resize(Display2, Display2, cDisplaySize)
        ib_mask_01.Image = Display2.Clone

        '6. Watershed zu objekt Auswertung  
        Dim useImage As Mat = tmp_LaplaceDepth
        If rb_Auswertung_Kombi.Checked Then
            useImage = tmp_LaplaceColor
        End If

        If Not WM_Watershed_Objekterkennung(tmp_Maske, useImage, tmp_Watershed, _MatWatershedMask, Konturen) Then
            lb_Info.Items.Insert(0, "Fehler bei: WM_Watershed_Objekterkennung")
            Return False
        End If

        '7. Draw Points
        If cb_DrawPoint.Checked Then
            If Not WM_DrawPoints(tmp_Watershed, tmp_Points) Then
                lb_Info.Items.Insert(0, "Fehler bei: WM_DrawPoints")
                Return False
            End If
            _MatPoints = tmp_Points.Clone
            CvInvoke.Resize(tmp_Points, Display1, cDisplaySize)
            ib_points_01.Image = Display1.Clone
        End If
        result = tmp_Watershed.Clone
        TC2_Bilder.SelectedTab = P2_Result
        TC3_ObjLists.SelectedTab = P1_All
        Return True
    End Function

    Private Function Search() As Boolean
        '1. Objektprüfen und Holen
        Dim AktSearch As MySearchObj
        lb_Found.Items.Clear()
        _MyMatchObjekts.Clear()

        Try
            AktSearch = _MySearchObjekte.ElementAt(CInt(num_SearchObj.Value - 1))
        Catch ex As Exception
            Try
                AktSearch = _MySearchObjekte.Last
                num_SearchObj.Value = _MySearchObjekte.IndexOf(_MySearchObjekte.Last) + 1
            Catch ex1 As Exception
                lb_Info.Items.Insert(0, "Keine objekte Gefunden")
                TC2_Bilder.SelectedTab = P2_Result
                TC3_ObjLists.SelectedTab = P1_All
                Return False
            End Try
        End Try

        '2. Objektinfos Anzeigen
        lbl_InfoName.Text = AktSearch.Name
        lbl_InfoMaase.Text = AktSearch.Maase
        'Alte Objekte aus liste Löschen
        lb_Found.Items.Clear()

        '3. Bildauswerung starten
        ImgageAnalyse()

        '4. Gefundene Objekte vergleichen 
        'Mase von mm in pixel wandeln
        Dim h As Int32 = MilToPix(AktSearch.Höhe)
        Dim b As Int32 = MilToPix(AktSearch.Beite)
        Dim t As Int32 = MilToPix(AktSearch.Tiefe)
        lb_Info.Items.Insert(0, $"h:{h,4} b:{b,4} t:{t,4}")
        For Each obj As MyObjektV2 In _MyObjekte
            'If obj.Passend(h, b, t, CInt(Num_SearchToleranz.Value)) Then
            Dim Ausrichtung As String
            Dim Fläche_HB, Fläche_BT, Fläche_HT, Abweichung, Toleranz As Double
            Fläche_HB = h * b
            Fläche_BT = b * t
            Fläche_HT = h * t
            lb_Info.Items.Insert(0, $"Gesuchte Flächen: HB{Fläche_HB,3} BT{Fläche_BT,3} HT{Fläche_HT,3}")
            If obj.PassendFläche(Fläche_HB, Fläche_BT, Fläche_HT, CInt(Num_SearchToleranz.Value)) Then
                'Prüfung zu welcher Fläche die Abweichung am gerinsten ist
                If Math.Abs(100 - ((100 / Fläche_HB) * obj.GetFläche)) < Math.Abs(100 - ((100 / Fläche_BT) * obj.GetFläche)) Then
                    If Math.Abs(100 - ((100 / Fläche_HB) * obj.GetFläche)) < Math.Abs(100 - ((100 / Fläche_HT) * obj.GetFläche)) Then
                        Abweichung = Math.Abs(100 - ((100 / Fläche_HB) * obj.GetFläche))
                        Ausrichtung = "HB"
                    Else
                        Abweichung = Math.Abs(100 - ((100 / Fläche_HT) * obj.GetFläche))
                        Ausrichtung = "HT"
                    End If
                Else
                    If Math.Abs(100 - ((100 / Fläche_BT) * obj.GetFläche)) < Math.Abs(100 - ((100 / Fläche_HT) * obj.GetFläche)) Then
                        Abweichung = Math.Abs(100 - ((100 / Fläche_BT) * obj.GetFläche))
                        Ausrichtung = "BT"
                    Else
                        Abweichung = Math.Abs(100 - ((100 / Fläche_HT) * obj.GetFläche))
                        Ausrichtung = "HT"
                    End If
                End If
                _MyMatchObjekts.Add(New MyMatchObj(Abweichung, Ausrichtung, obj))
            End If
            'End If
        Next

        '5. Passendes Objekt Anzeigen
        If _MyMatchObjekts.Count <= 0 Then
            lb_Info.Items.Insert(0, "Es wurde kein passendes Objekt gefunden.")
            TC2_Bilder.SelectedTab = P2_Result
            TC3_ObjLists.SelectedTab = P1_All
            Return False
        End If
        lb_Info.Items.Insert(0, $"Es wurde {_MyMatchObjekts.Count,3} passendes Objekt gefunden.")
        _MyMatchObjekts.Sort()
        For Each obj In _MyMatchObjekts
            lb_Found.Items.Add(obj.ToString())
        Next
        lbl_FoundObj.Text = _MyMatchObjekts(0).Objekt.ToString
        Dim mm As Int32 = CInt(_MyMatchObjekts(0).Objekt.Dist_Max() / num_pixmmH_faktor.Value)
        lbl_FoundWidth.Text = $"{_MyMatchObjekts(0).Objekt.Dist_Max(),4} pixel = {mm,4} mm"
        lbl_FoundZent.Text = $"{_MyMatchObjekts(0).Objekt.GetZentrumPoint.ToString()}"
        lbl_Found_Rot.Text = $"{_MyMatchObjekts(0).Objekt.GetWinkel2.ToString()}"
        lbl_Found_Depth.Text = _MyMatchObjekts(0).Objekt.GetDepthStr()
        TC2_Bilder.SelectedTab = P5_ResultSearchObj
        TC3_ObjLists.SelectedTab = P2_Found
        '6. Zeichnen
        Dim ZeichenMat3 As New Mat
        ZeichenMat3 = Mat.Zeros(_MatResult.Rows, _MatResult.Cols, DepthType.Cv8U, 3)
        For Each obj In _MyMatchObjekts
            Dim o As MyObjektV2 = obj.Objekt
            CvInvoke.DrawContours(ZeichenMat3, o.GetContours, 0, New MCvScalar(o.Color(0), o.Color(1), o.Color(2)), -1)
            Dim pin As Point()
            pin = o.GetMinAreaPoints()
            CvInvoke.Circle(ZeichenMat3, pin(0), 3, New MCvScalar(255, 0, 0), 3)
            'draw Boxes
            For i = 0 To 2
                CvInvoke.Line(ZeichenMat3, pin(i), pin((i + 1)), New MCvScalar(0, 0, 255), 2) 'min
                'Grundlinie
                CvInvoke.Line(ZeichenMat3, pin(3), pin((0)), New MCvScalar(255, 255, 255), 2) 'min
            Next
            CvInvoke.PutText(ZeichenMat3, $"ID:{o.ID,2} Winkel:{o.GetWinkel2,4:n2}", o.GetZentrumPoint, FontFace.HersheyComplex, 1, New MCvScalar(255, 255, 255))
        Next
        CvInvoke.Resize(ZeichenMat3, ZeichenMat3, New Drawing.Size(640, 480))
        ib_Found.Image = ZeichenMat3.Clone

        '7. werte senden
        Dim Mobj As MyMatchObj = _MyMatchObjekts(0)
        Dim Point As MyPoint = Mobj.Objekt.GetZentrumMyPoint(_MatDepth)
        Dim Winkel As Double = Mobj.Objekt.GetWinkel2()
        Dim depth As Double = Mobj.Objekt.GetDepthVal
        'Pos
        If _TcpVariablen.Exists("x") Then
            _TcpVariablen.SetVariable("x", PixToMil(Point.X) * num_RoboOffsetX.Value)
        Else
            lb_Info.Items.Insert(0, $"Fehler Kommunikation die TCPVariable ""x"" existiert nicht")
        End If
        If _TcpVariablen.Exists("y") Then
            _TcpVariablen.SetVariable("y", PixToMil(Point.Y) + num_RoboOffsety.Value)
        Else
            lb_Info.Items.Insert(0, $"Fehler Kommunikation die TCPVariable ""y"" existiert nicht")
        End If
        If _TcpVariablen.Exists("z") Then
            _TcpVariablen.SetVariable("z", depth)
        Else
            lb_Info.Items.Insert(0, $"Fehler Kommunikation die TCPVariable ""z"" existiert nicht")
        End If
        'Höhe Tiefe
        If _TcpVariablen.Exists("h") Then
            If Mobj.Ausrichtung = "HB" Then
                _TcpVariablen.SetVariable("h", AktSearch.Tiefe)
            Else
                If Mobj.Ausrichtung = "HT" Then
                    _TcpVariablen.SetVariable("h", AktSearch.Beite)
                Else
                    _TcpVariablen.SetVariable("h", AktSearch.Höhe)
                End If
            End If
        Else
            lb_Info.Items.Insert(0, $"Fehler Kommunikation die TCPVariable ""h"" existiert nicht")
        End If

        If _TcpVariablen.Exists("b") Then
            If Mobj.Ausrichtung = "HB" Or Mobj.Ausrichtung = "BT" Then
                _TcpVariablen.SetVariable("b", AktSearch.Beite)
            Else
                _TcpVariablen.SetVariable("b", AktSearch.Höhe)
            End If
        Else
            lb_Info.Items.Insert(0, $"Fehler Kommunikation die TCPVariable ""b"" existiert nicht")
        End If
        If _TcpVariablen.Exists("t") Then
            If Mobj.Ausrichtung = "BT" Or Mobj.Ausrichtung = "HT" Then
                _TcpVariablen.SetVariable("t", AktSearch.Tiefe)
            Else
                _TcpVariablen.SetVariable("t", AktSearch.Höhe)

            End If
        Else
            lb_Info.Items.Insert(0, $"Fehler Kommunikation die TCPVariable ""t"" existiert nicht")
        End If
        'Winkel
        If _TcpVariablen.Exists("a") Then
            _TcpVariablen.SetVariable("a", Winkel)
        Else
            lb_Info.Items.Insert(0, $"Fehler Kommunikation die TCPVariable ""a"" existiert nicht")
        End If
        Return True
    End Function

    Private Function Auto() As Double
        If TakePicture(_MatColor, _MatDepth, _MatDepthC) Then
            If Search() Then
                Return 1
            End If
        End If
        Return -1
    End Function
    '----------------------------------------------------------------------------------------------------------------------------
    'Watershed Module
    '----------------------------------------------------------------------------------------------------------------------------

    Private Function WM_ImageOfInterest(ByRef resurce As Mat, ByRef result As Mat, Optional rev As Mat = Nothing, Optional dif As Boolean = False, Optional mask As Boolean = False, Optional depth As Boolean = False, Optional gaus As Boolean = False, Optional offset As Int32 = 0) As Boolean
        Dim Ergebnis As Boolean = True
        Dim tmp_Mat As New Mat
        tmp_Mat = resurce.Clone
        'Differenz
        If dif And Ergebnis Then
            If rev IsNot Nothing Then
                CvInvoke.AbsDiff(tmp_Mat, rev, tmp_Mat)
            Else
                lb_Info.Items.Insert(0, "IOIFehler keine Reverenz übergeben")
                Ergebnis = False
            End If
        End If
        'Tiefenfilder
        If depth And Ergebnis Then
            CvInvoke.Threshold(tmp_Mat, tmp_Mat, CInt(num_ThreshHoch.Value), 100, ThresholdType.ToZero)
            CvInvoke.Threshold(tmp_Mat, tmp_Mat, CInt(num_ThreshTief.Value), 100, ThresholdType.ToZeroInv)
        End If
        'Maskierung
        If mask And Ergebnis Then
            tmp_Mat = Maskieren(tmp_Mat, CInt(num_MaskH.Value), CInt(num_MaskV.Value), offset)
        End If
        'Gaus
        If gaus And Ergebnis Then
            CvInvoke.GaussianBlur(tmp_Mat, tmp_Mat, New Drawing.Size(3, 3), 2)
        End If
        result = tmp_Mat.Clone
        Return Ergebnis
    End Function

    Private Function WM_LaplaceFiltering(ByRef resurce As Mat, ByRef result As Mat) As Boolean
        Dim tmp_Mat As New Mat
        tmp_Mat = resurce.Clone
        Dim tmpResul As New Mat
        Dim Kernel As New Matrix(Of Single)(New Single(,) {
                                            {1, 1, 1},
                                            {1, -8, 1},
                                            {1, 1, 1}})
        Dim Laplace32F As New Mat(tmp_Mat.Size, DepthType.Cv32F, tmp_Mat.NumberOfChannels)
        Dim Result32F As New Mat(tmp_Mat.Size, DepthType.Cv32F, tmp_Mat.NumberOfChannels)
        CvInvoke.Filter2D(tmp_Mat, Laplace32F, Kernel, New Point(-1, -1))
        Dim IMG32F As New Mat : tmp_Mat.ConvertTo(IMG32F, DepthType.Cv32F)
        CvInvoke.Subtract(IMG32F, Laplace32F, Result32F)

        'zurückwandeln
        Result32F.ConvertTo(tmpResul, resurce.Depth)
        result = tmpResul.Clone
        Return True
    End Function

    Private Function WM_Graustufen_Binärbild(ByRef resurce As Mat, ByRef result As Mat, Optional threshholdvalue As Double = 10.0) As Boolean
        Dim tmp_Mat As New Mat
        tmp_Mat = resurce.Clone
        Dim GrayImg As New Mat

        If tmp_Mat.NumberOfChannels = 3 Then
            CvInvoke.CvtColor(tmp_Mat, tmp_Mat, ColorConversion.Bgr2Gray, 1)
        End If
        tmp_Mat.ConvertTo(GrayImg, DepthType.Cv8U)

        CvInvoke.Threshold(GrayImg, GrayImg, threshholdvalue, 255, ThresholdType.Binary Or ThresholdType.Otsu) 'benötigt cv8u
        result = GrayImg.Clone
        Return True
    End Function

    Private Function WM_DistanceDetection(ByRef resurce As Mat, ByRef resultobjekts As Mat, ByRef resultbackground As Mat, Optional threshholdvalueObj As Double = 0.1, Optional threshholdvalueBack As Double = 0.3) As Boolean
        Dim tmp_Mat As New Mat
        tmp_Mat = resurce.Clone
        Dim tmp_MatInv As New Mat
        CvInvoke.BitwiseNot(tmp_Mat, tmp_MatInv)
        'Distanc Funktion & normalize for objects
        CvInvoke.DistanceTransform(tmp_Mat, tmp_Mat, Nothing, DistType.L2, 3) 'Mask size muss 0 || 3 ||5 sein  'CvInvoke.DistanceTransform(BinaerImg, DistImg, Nothing, DistType.L2, 3)
        CvInvoke.Normalize(tmp_Mat, tmp_Mat, 0, 1.0, NormType.MinMax)
        'Distanc Funktion & normalize for background
        CvInvoke.DistanceTransform(tmp_MatInv, tmp_MatInv, Nothing, DistType.L2, 3) 'Mask size muss 0 || 3 ||5 sein  'CvInvoke.DistanceTransform(BinaerImg, DistImg, Nothing, DistType.L2, 3)
        CvInvoke.Normalize(tmp_MatInv, tmp_MatInv, 0, 1.0, NormType.MinMax)


        Dim kernel1 = Mat.Ones(3, 3, DepthType.Cv8U, 1)
        'Bild bereinigen (mit Treshold) und in CV8 wandeln objects
        CvInvoke.Threshold(tmp_Mat, tmp_Mat, threshholdvalueObj, 1.0, ThresholdType.Binary) '0.3
        CvInvoke.Dilate(tmp_Mat, tmp_Mat, kernel1, New Point(-1, -1), 1, BorderType.Default, New MCvScalar(0))
        CvInvoke.Normalize(tmp_Mat, tmp_Mat, 0, 255, NormType.MinMax, DepthType.Cv8U)
        'Bild bereinigen (mit Treshold) und in CV8 wandeln background
        CvInvoke.Threshold(tmp_MatInv, tmp_MatInv, threshholdvalueBack, 1.0, ThresholdType.Binary) '0.3
        CvInvoke.Dilate(tmp_MatInv, tmp_MatInv, kernel1, New Point(-1, -1), 1, BorderType.Default, New MCvScalar(0))
        CvInvoke.Normalize(tmp_MatInv, tmp_MatInv, 0, 255, NormType.MinMax, DepthType.Cv8U)
        resultobjekts = tmp_Mat.Clone
        resultbackground = tmp_MatInv.Clone
        Return True
    End Function

    Private Function WM_MarkObjects(ByRef resurceObjects As Mat, ByRef resurceBackground As Mat, ByRef result As Mat, ByRef ObjKonturen As VectorOfVectorOfPoint) As Boolean
        Dim ContoursBack As New VectorOfVectorOfPoint
        Dim ContoursObj As New VectorOfVectorOfPoint
        Dim Markers As New Mat(resurceObjects.Size, DepthType.Cv32S, 1) ' Zur Anzeige farbiger Bilder
        Markers.SetTo(New MCvScalar(0)) ' AnzeigenBild auf schwarz setzen. 0 = undefinierter Bereich
        If resurceObjects.Size <> resurceBackground.Size Or resurceObjects.Depth <> resurceBackground.Depth Or resurceObjects.NumberOfChannels <> resurceBackground.NumberOfChannels Then
            result = Markers.Clone
            lb_Info.Items.Insert(0, $"MarkObjects: Fehler ObjektMaske und Hintergrund Maske passen nicht aufeinander")
            Return False
        End If
        'Background bezeichnen
        CvInvoke.FindContours(resurceBackground, ContoursBack, Nothing, RetrType.List, ChainApproxMethod.ChainApproxSimple)
        lb_Info.Items.Insert(0, $"Es wurden {ContoursBack.Size} Hintergrundkontuen Gefunden")
        For i = 0 To ContoursBack.Size - 1
            Dim Colo = New MCvScalar(255) ' -1 ist der Hintergrund
            CvInvoke.DrawContours(Markers, ContoursBack, i, Colo, -1) ' -1 damit die contour ausgefüllt wird
        Next
        'Objekte bezeichnen
        CvInvoke.FindContours(resurceObjects, ContoursObj, Nothing, RetrType.List, ChainApproxMethod.ChainApproxSimple)
        lb_Info.Items.Insert(0, $"Es wurden {ContoursObj.Size} Objektkontuen Gefunden")
        For i = 0 To ContoursObj.Size - 1
            Dim Colo = New MCvScalar((i + 1) * 10) ' An 1 nummerieren. 0 ist ja der Hintergrund
            CvInvoke.DrawContours(Markers, ContoursObj, i, Colo, -1) ' -1 damit die contour ausgefüllt wird
        Next
        If ContoursBack.Size <= 0 Or ContoursObj.Size <= 0 Then
            lb_Info.Items.Insert(0, $"MarkObjects: Fehler es wurden nur {ContoursObj.Size} Objekt- und {ContoursBack.Size} Hintergrundkontuen Gefunden")
            result = Markers.Clone
            Return False
        End If
        ObjKonturen = ContoursObj
        result = Markers.Clone
        Return True
    End Function

    Private Function WM_Watershed_Objekterkennung(ByRef resurceMaske As Mat, ByRef resurceBild As Mat, ByRef result As Mat, ByRef resultMask As Mat, ByRef objKonturen As VectorOfVectorOfPoint) As Boolean
        Dim tmp_Markers As New Mat
        tmp_Markers = resurceMaske.Clone
        Dim imgWater8U3 As New Mat

        'Watershed zu objekt Auswertung  Unklar ob Result8U oder DistImg8u
        If resurceBild.Depth <> DepthType.Cv8U Then
            resurceBild.ConvertTo(resurceBild, DepthType.Cv8U)
        End If
        If resurceBild.NumberOfChannels = 3 Then

            imgWater8U3 = resurceBild
        Else
            CvInvoke.CvtColor(resurceBild, imgWater8U3, ColorConversion.Gray2Bgr, 3)
        End If

        ' CvInvoke.WaitKey(0)
        CvInvoke.Watershed(imgWater8U3, tmp_Markers)
        'Farbenvergeben
        Dim Nc = objKonturen.Size ' Anzahl Contours = Anzahl an markierten Elementen
        Dim Cols(Nc) As MCvScalar : Dim RND As New Random 'Dim Cols(Nc - 1) As MCvScalar : Dim RND As New Random
        For i = 0 To Nc '- 1
            Cols(i) = New Bgr(RND.Next(0, 256), RND.Next(0, 256), RND.Next(0, 256)).MCvScalar
        Next

        Dim ZeichenMat2 = Mat.Zeros(tmp_Markers.Rows, tmp_Markers.Cols, DepthType.Cv8U, 3)
        For Zeile = 0 To tmp_Markers.Rows - 1
            For Spalte = 0 To tmp_Markers.Cols - 1
                Dim Marke As Int32 = UmwandlungClass.GetInt32Value(tmp_Markers, Zeile, Spalte)
                Dim gefunden As Boolean = False
                If Marke > 0 And Marke <= Nc * 10 Then
                    Dim ByteWerte() As Byte = {CByte(Cols(Marke \ 10).V0), CByte(Cols(Marke \ 10).V1), CByte(Cols(Marke \ 10).V2)}
                    'Objekt eintragen
                    'Erstes Objekt
                    If _MyObjekte.Count < 1 Then
                        Dim tmpObjekt As New MyObjektV2(Marke, ByteWerte)
                        Dim tiefe As Int32 = UmwandlungClass.GetInt16Value(_MatDepth, Zeile, Spalte)
                        tmpObjekt.Add_Ref(Spalte, Zeile, tiefe)
                        _MyObjekte.Add(tmpObjekt)
                    Else
                        'Prüfen ob Objekt bereits Angelegt
                        For Each ob As MyObjektV2 In _MyObjekte
                            If Marke = ob.ID Then
                                gefunden = True
                                Dim tiefe As Int32 = UmwandlungClass.GetInt16Value(_MatDepth, Zeile, Spalte)
                                ob.Add_Ref(Spalte, Zeile, tiefe)
                            End If
                        Next
                        'Objekt neu Anlegen 
                        If Not gefunden Then
                            Dim tmpObjekt As New MyObjektV2(Marke, ByteWerte)
                            Dim tiefe As Int32 = UmwandlungClass.GetInt16Value(_MatDepth, Zeile, Spalte)
                            tmpObjekt.Add_Ref(Spalte, Zeile, tiefe)
                            _MyObjekte.Add(tmpObjekt)
                        End If
                    End If
                    UmwandlungClass.SetByteValues(ZeichenMat2, Zeile, Spalte, ByteWerte)
                End If
            Next
        Next
        lb_Info.Items.Insert(0, $"Es wurden {_MyObjekte.Count,3} Objekte erkannt")
        'Filtern
        If cb_Watershed_Filter.Checked Then
            Dim MinFlächeMM, MinFlächePix As Int32
            'Kürzeste Kanten finden und Kleinste Fläche berechnen
            If num_WTS_MinB.Value > num_WTS_MinH.Value Then
                If num_WTS_MinB.Value > num_WTS_MinT.Value Then
                    MinFlächeMM = CInt(num_WTS_MinH.Value * num_WTS_MinT.Value)
                Else
                    MinFlächeMM = CInt(num_WTS_MinH.Value * num_WTS_MinB.Value)
                End If
            Else
                If num_WTS_MinH.Value > num_WTS_MinT.Value Then
                    MinFlächeMM = CInt(num_WTS_MinB.Value * num_WTS_MinT.Value)
                Else
                    MinFlächeMM = CInt(num_WTS_MinB.Value * num_WTS_MinH.Value)
                End If
            End If
            MinFlächePix = MilToPix(MinFlächeMM)
            lb_Info.Items.Insert(0, $"Min Flävhe: {MinFlächeMM}mm² bzw. {MinFlächePix}pixel ")
            Dim entf As Int32 = 0
            Dim zulöschen As New List(Of MyObjektV2)
            For Each obj As MyObjektV2 In _MyObjekte
                If obj.GetFläche < MinFlächePix Then
                    zulöschen.Add(obj)
                    entf += 1
                End If
            Next
            For Each obj As MyObjektV2 In zulöschen
                _MyObjekte.Remove(obj)
            Next
            lb_Info.Items.Insert(0, $"Es wurden {entf,3} Objekte gefiltert")
            lb_Info.Items.Insert(0, $"Es gibt {_MyObjekte.Count,3} interesante Objekte")
        End If

        'Eintagen
        For Each ob2 As MyObjektV2 In _MyObjekte
            LB_obj.Items.Add(ob2.ToString)
            'Zentrum
            CvInvoke.Circle(ZeichenMat2, ob2.GetZentrumPoint, 5, New MCvScalar(255, 255, 255), 2)
            CvInvoke.Circle(ZeichenMat2, ob2.GetZentrumPoint, 1, New MCvScalar(255, 0, 255), 1)

            Dim pin As Point()
            pin = ob2.GetMinAreaPoints()
            '0-Punkt
            CvInvoke.Circle(ZeichenMat2, pin(0), 3, New MCvScalar(255, 255, 0), 2)
            'draw Boxes
            For i = 0 To 2
                CvInvoke.Line(ZeichenMat2, pin(i), pin((i + 1)), New MCvScalar(0, 0, 255), 2) 'min
            Next
            CvInvoke.Line(ZeichenMat2, pin(3), pin(0), New MCvScalar(255, 255, 255), 2) 'Grundlinie
            CvInvoke.PutText(ZeichenMat2, $"ID:{ob2.ID,2}", ob2.GetZentrumPoint, FontFace.HersheyComplex, 1, New MCvScalar(255, 255, 255), 2)


        Next
        result = ZeichenMat2.Clone
        resultMask = tmp_Markers.Clone
        If _MyObjekte.Count <= 0 Then
            lb_Info.Items.Insert(0, " Watershed_Objekterkennung: Fehler es wurden keine Objekte erkannt")
            Return False
        End If
        Return True
    End Function

    Private Function WM_DrawPoints(ByRef resurce As Mat, ByRef result As Mat) As Boolean
        Dim tmp_Mat As New Mat
        tmp_Mat = resurce.Clone
        'Draw Points
        For Each obj As MyObjektV2 In _MyObjekte
            'X min => Grün
            CvInvoke.Circle(tmp_Mat, New Point(obj.Min_X.X, obj.Min_X.Y), 6, New MCvScalar(255, 255, 255), 3)
            CvInvoke.Circle(tmp_Mat, New Point(obj.Min_X.X, obj.Min_X.Y), 1, New MCvScalar(0, 255, 0), 1) 'Gün
            'X max => GrünRot
            CvInvoke.Circle(tmp_Mat, New Point(obj.Max_X.X, obj.Max_X.Y), 6, New MCvScalar(255, 255, 255), 3)
            CvInvoke.Circle(tmp_Mat, New Point(obj.Max_X.X, obj.Max_X.Y), 1, New MCvScalar(0, 255, 255), 1) 'Gün
            'Y min => Blau
            CvInvoke.Circle(tmp_Mat, New Point(obj.Min_Y.X, obj.Min_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
            CvInvoke.Circle(tmp_Mat, New Point(obj.Min_Y.X, obj.Min_Y.Y), 1, New MCvScalar(255, 0, 0), 1) 'Blau
            'Y max => BlauRot
            CvInvoke.Circle(tmp_Mat, New Point(obj.Max_Y.X, obj.Max_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
            CvInvoke.Circle(tmp_Mat, New Point(obj.Max_Y.X, obj.Max_Y.Y), 1, New MCvScalar(255, 0, 255), 1) 'Blau
            'Virtueller Start => Rot
            CvInvoke.Circle(tmp_Mat, New Point(obj.Min_X.X, obj.Min_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
            CvInvoke.Circle(tmp_Mat, New Point(obj.Min_X.X, obj.Min_Y.Y), 1, New MCvScalar(0, 0, 255), 1) 'Rot
            'Virtuelles Ende => Rotgrünblau
            CvInvoke.Circle(tmp_Mat, New Point(obj.Max_X.X, obj.Max_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
            CvInvoke.Circle(tmp_Mat, New Point(obj.Max_X.X, obj.Max_Y.Y), 1, New MCvScalar(125, 125, 255), 1) 'Rot
            'Virtuelle Ecke3 => Rot
            CvInvoke.Circle(tmp_Mat, New Point(obj.Max_X.X, obj.Min_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
            CvInvoke.Circle(tmp_Mat, New Point(obj.Max_X.X, obj.Min_Y.Y), 1, New MCvScalar(0, 0, 255), 1) 'Rot
            'Virtuelle Ecke4 => Rotgrünblau
            CvInvoke.Circle(tmp_Mat, New Point(obj.Min_X.X, obj.Max_Y.Y), 6, New MCvScalar(255, 255, 255), 3)
            CvInvoke.Circle(tmp_Mat, New Point(obj.Min_X.X, obj.Max_Y.Y), 1, New MCvScalar(125, 125, 255), 1) 'Rot

            'MaxKontur zeichnen
            CvInvoke.Line(tmp_Mat, New Point(obj.Min_X.X, obj.Min_Y.Y), New Point(obj.Max_X.X, obj.Min_Y.Y), New MCvScalar(0, 255, 0), 2)
            CvInvoke.Line(tmp_Mat, New Point(obj.Max_X.X, obj.Min_Y.Y), New Point(obj.Max_X.X, obj.Max_Y.Y), New MCvScalar(0, 255, 0), 2)
            CvInvoke.Line(tmp_Mat, New Point(obj.Max_X.X, obj.Max_Y.Y), New Point(obj.Min_X.X, obj.Max_Y.Y), New MCvScalar(0, 255, 0), 2)
            CvInvoke.Line(tmp_Mat, New Point(obj.Min_X.X, obj.Max_Y.Y), New Point(obj.Min_X.X, obj.Min_Y.Y), New MCvScalar(0, 255, 0), 2)

        Next
        result = tmp_Mat.Clone
        Return True
    End Function


    '----------------------------------------------------------------------------------------------------------------------------
    'LÖschen Tests
    '----------------------------------------------------------------------------------------------------------------------------
    Private Function KantenFinden(ByRef resurceMask As Mat, ByRef resurceImg As Mat) As Boolean
        Dim tmp_Mat As New Mat
        tmp_Mat = resurceMask.Clone
        Dim tmp_Img As New Mat
        tmp_Img = resurceImg.Clone
        tmp_Mat.ConvertTo(tmp_Mat, DepthType.Cv8U)
        CvInvoke.CvtColor(tmp_Mat, tmp_Mat, ColorConversion.Gray2Bgr, 3)
        CvInvoke.Imshow("t1", tmp_Mat)
        CvInvoke.Threshold(tmp_Mat, tmp_Mat, 250, 255, ThresholdType.BinaryInv)
        CvInvoke.CvtColor(tmp_Mat, tmp_Mat, ColorConversion.Bgr2Gray, 1)
        CvInvoke.Imshow("t2", tmp_Mat)
        Dim mycontures As New VectorOfVectorOfPoint
        CvInvoke.FindContours(tmp_Mat, mycontures, Nothing, RetrType.List, ChainApproxMethod.ChainApproxSimple)
        CvInvoke.Imshow("t3", tmp_Mat)

        'Filtern
        Dim MinFlächeMM, MinFlächePix As Int32
        If cb_Watershed_Filter.Checked Then
            'Kürzeste Kanten finden und Kleinste Fläche berechnen
            If num_WTS_MinB.Value > num_WTS_MinH.Value Then
                If num_WTS_MinB.Value > num_WTS_MinT.Value Then
                    MinFlächeMM = CInt(num_WTS_MinH.Value * num_WTS_MinT.Value)
                Else
                    MinFlächeMM = CInt(num_WTS_MinH.Value * num_WTS_MinB.Value)
                End If
            Else
                If num_WTS_MinH.Value > num_WTS_MinT.Value Then
                    MinFlächeMM = CInt(num_WTS_MinB.Value * num_WTS_MinT.Value)
                Else
                    MinFlächeMM = CInt(num_WTS_MinB.Value * num_WTS_MinH.Value)
                End If
            End If
            MinFlächePix = MilToPix(MinFlächeMM)
            lb_Info.Items.Insert(0, $"Min Flävhe: {MinFlächeMM}mm² bzw. {MinFlächePix}pixel ")
        Else
            MinFlächePix = 0
        End If


        For i = 0 To mycontures.Size - 1
            Dim myFinalRec As New RotatedRect

            myFinalRec = CvInvoke.MinAreaRect(mycontures(i))

            'Dim wert As Int32 = UmwandlungClass.GetInt32Value(resurceMask, CInt(myFinalRec.Center.X), CInt(myFinalRec.Center.Y))
            Dim wert As Double = myFinalRec.Angle
            myFinalRec.GetVertices()
            Dim punkte() As PointF = myFinalRec.GetVertices()
            Dim HöhePix As Int32 = CInt(Math.Round(Math.Sqrt((punkte(1).X - punkte(0).X) ^ 2 + (punkte(1).Y - punkte(0).Y) ^ 2)))
            Dim BreitePix As Int32 = CInt(Math.Round(Math.Sqrt((punkte(2).X - punkte(1).X) ^ 2 + (punkte(2).Y - punkte(1).Y) ^ 2)))
            Dim fläche As Int32 = HöhePix * BreitePix

            lb_Info.Items.Insert(0, $"MinAreaRec: {i,3} H:{HöhePix,4} B:{BreitePix,4} F:{fläche,4}")
            If fläche >= MinFlächePix Then
                lb_Info.Items.Insert(0, $"MinAreaRec: {i,3} OK")
                CvInvoke.PutText(tmp_Img, wert.ToString("N2"), New Point(CInt(myFinalRec.Center.X), CInt(myFinalRec.Center.Y)), FontFace.HersheyComplex, 1, New MCvScalar(255, 255, 255))
                For j = 0 To 3
                    CvInvoke.Line(tmp_Img, New Point(CInt(punkte(j).X), CInt(punkte(j).Y)), New Point(CInt(punkte((j + 1) Mod 4).X), CInt(punkte((j + 1) Mod 4).Y)), New MCvScalar(0, 0, 255))
                Next
            Else
                lb_Info.Items.Insert(0, $"MinAreaRec: {i,3} ZU KLEIN")
            End If


        Next
        CvInvoke.Imshow("minAreaRec", tmp_Img)


        'Dim Mat2, mat3 As New Mat
        'Dim GrayImg As New Mat
        'If tmp_Mat.NumberOfChannels = 3 Then
        '    CvInvoke.CvtColor(tmp_Mat, GrayImg, ColorConversion.Bgr2Gray, 1)
        'End If
        'GrayImg.ConvertTo(GrayImg, DepthType.Cv8U)


        'CvInvoke.Canny(tmp_Mat, Mat2, 50, 200)
        'CvInvoke.Imshow("canny", Mat2)
        ''CvInvoke.CvtColor(Mat2, mat3, ColorConversion.Gray2Bgr)
        'Dim Lines As New VectorOfVectorOfPointF
        ''CvInvoke.HoughLines(Mat2, Lines, 1, Math.PI / 180, 50, 0, 0)

        'Dim listVecs() As LineSegment2D
        'listVecs = CvInvoke.HoughLinesP(Mat2, 1, Math.PI / 180, 25, 25, 20)
        'lb_Info.Items.Insert(0, $"Es wurden {listVecs.Length} Kanten gezeichnet")
        'For i = 0 To listVecs.Length - 1
        '    Dim segment As LineSegment2D = listVecs(i)

        '    Dim Marke(8) As Int32
        '    Dim Objekt As Int32 = 0
        '    Dim j As Int32 = 0
        '    'Um Punktherum suchen 
        '    For x = segment.P1.X - 2 To segment.P1.X + 2
        '        For y = segment.P1.Y - 2 To segment.P1.Y + 2
        '            If x > 0 And x <= resurceMask.Cols And y > 0 And y <= resurceMask.Rows Then
        '                Try
        '                    Marke(j) = UmwandlungClass.GetInt32Value(resurceMask, x, y)
        '                Catch ex As Exception

        '                End Try

        '            End If
        '            j += 1
        '        Next
        '    Next

        '    For k = 0 To 8
        '        If Marke(k) <> 0 And Marke(k) <> 255 Then
        '            If Objekt = 0 Or Objekt = Marke(k) Then
        '                Objekt = Marke(k)
        '            Else
        '                lb_Info.Items.Insert(0, $"Linie: {i,3} Objekt mismatch  {Objekt,3} und {Marke(k),3}")
        '                Objekt = Marke(k)
        '            End If
        '        End If
        '    Next
        '    Dim gefunden As Boolean = False
        '    For Each obj As MyObekt In _MyObjekte
        '        If Objekt = obj.ID Then
        '            gefunden = True
        '            lb_Info.Items.Insert(0, $"Linie: {i,3} gehört zu Objekt {obj.ID,3}")
        '        End If
        '    Next
        '    If Not gefunden Then
        '        lb_Info.Items.Insert(0, $"Linie: {i,3} passt zu keinem Objekt")
        '    End If
        '    CvInvoke.Line(tmp_Mat, segment.P1, segment.P2, New MCvScalar(0, 0, 255))
        'Next
        'Dim v2 As New ImageViewer
        'v2.Image = tmp_Mat.Clone : v2.Text = "Line" : v2.Show()

    End Function

    Private Sub btn_Canny_Click(sender As Object, e As EventArgs) Handles btn_Canny.Click
        CvInvoke.Canny(_MatColor, _MatResult, 100, 200)
        ib_res_01.Image = _MatColor.Clone
        ib_res_02.Image = _MatResult.Clone
        TC2_Bilder.SelectedTab = P2_Result
    End Sub

    Private Sub _ClearList(list As IList, Optional listbox As ListBox = Nothing)
        list.Clear()
        If listbox IsNot Nothing Then
            _RefreshListbox(listbox, list)
        End If
    End Sub
    Private Sub _RefreshListbox(listbox As ListBox, list As IList)
        listbox.Items.Clear()
        For Each obj In list
            listbox.Items.Add(obj)
        Next
    End Sub


End Class 'Form1