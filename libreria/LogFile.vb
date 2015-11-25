Imports System.IO
Imports System.Reflection.Assembly
Imports System.Threading
Imports ICSharpCode.SharpZipLib.Zip
Imports ICSharpCode.SharpZipLib.Checksums

''' <summary>
''' Clase para la creacion y manejo de archivos log
''' </summary>
''' <remarks></remarks>
Public Class LogFile

    Private _archivo As String
    Private _hora As Boolean = True
    Private _t_zip As Integer = 0
    Private Thread_Zip As Thread
    Private z_f As String
    Public Event mensaje(ByVal texto As String)

    ''' <summary>
    ''' Crea un nuevo objeto LogFile
    ''' </summary>
    ''' <param name="NombreArchivo">Nombre que tendra el archivo Log</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal NombreArchivo As String)
        Me.Archivo = NombreArchivo
        Me.check_dir(Me.ruta_log)
        Me.check_archivo()
    End Sub

    ''' <summary>
    ''' Crea un nuevo objeto LogFile
    ''' </summary>
    ''' <param name="NombreArchivo">Nombre que tendra el archivo Log</param>
    ''' <param name="FechaAuto">Indica si debe agregarse la hora al mensaje en el archivo</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal NombreArchivo As String, ByVal FechaAuto As Boolean)
        Me.Archivo = NombreArchivo
        Me.ColocarHora = FechaAuto
        Me.check_dir(Me.ruta_log)
        Me.check_archivo()
    End Sub

    Public Sub New(ByVal NombreArchivo As String, ByVal FechaAuto As Boolean, ByVal TamañoZip As Integer)
        Me.Archivo = NombreArchivo
        Me.ColocarHora = FechaAuto
        Me.ZipLargo = TamañoZip
        Me.check_dir(Me.ruta_log)
        Me.check_archivo()
    End Sub

    ''' <summary>
    ''' Ruta por defecto de los archivos Log
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function ruta_log() As String
        Return System.AppDomain.CurrentDomain.BaseDirectory() & "log\"
    End Function

    ''' <summary>
    ''' Verifica si el directorio indicado existe. Si no, lo crea automaticamente
    ''' </summary>
    ''' <param name="directorio">Ruta del diretorio como un String</param>
    ''' <remarks></remarks>
    Private Sub check_dir(ByVal directorio As String)
        Try
            If Not Directory.Exists(directorio) Then
                Directory.CreateDirectory(directorio)
            End If
        Catch ex As Exception
            Me.mensaje_log(ex.Message)
        End Try
    End Sub

    Private Sub check_archivo()
        If Me._archivo = "" Then
            Me._archivo = "LogFile.log"
        End If
        If Not File.Exists(Me.ruta_log() & Me._archivo) Then
            Me.escribir(Me.Archivo & " Log Iniciado")
        End If
    End Sub

    Private Sub mensaje_log(ByVal texto As String)
        RaiseEvent mensaje(texto)
    End Sub

    Public Property ZipLargo() As Integer
        Get
            Return Me._t_zip
        End Get
        Set(ByVal value As Integer)
            Me._t_zip = value
        End Set
    End Property

    Public Property ColocarHora() As Boolean
        Get
            Return Me._hora
        End Get
        Set(ByVal value As Boolean)
            Me._hora = value
        End Set
    End Property

    Public Property Archivo() As String
        Get
            Return Me._archivo
        End Get
        Set(ByVal value As String)
            Dim i As New IO.FileInfo(value)
            Select Case i.Extension.ToUpper
                Case ""
                    _archivo = value & ".log"
                Case Is <> ".LOG"
                    _archivo = value.Replace(i.Extension, ".log")
                Case Else
                    _archivo = value
            End Select
        End Set
    End Property

    Private Function TextoH() As String
        Return Now.Year.ToString.PadLeft(2, "0") & Now.Month.ToString.PadLeft(2, "0") & Now.Day.ToString.PadLeft(2, "0") & Now.Hour.ToString.PadLeft(2, "0") & Now.Minute.ToString.PadLeft(2, "0") & Now.Second.ToString.PadLeft(2, "0")
    End Function

    ''' <summary>
    ''' Verifica si se especifico un tamaño maximo de archivo. Si el tamaño del archivo lo sobrepasa, lo comprime
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Zip() As Boolean
        Try
            If Me._t_zip > 0 Then
                If File.Exists(Me.ruta_log() & Me._archivo) Then
                    Dim i As New IO.FileInfo(Me.ruta_log() & Me._archivo)
                    If i.Length / 1024 / 1024 > Me._t_zip Then
                        Me.Comprimir_Log()
                        Return True
                    End If
                End If
            End If
        Catch ex As Exception
            Me.mensaje_log(ex.Message)
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Inicia el proceso de compresion del archivo en un zip
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub Comprimir_Log()
        Me.z_f = Me.TextoH() & "_" & Me._archivo
        My.Computer.FileSystem.RenameFile(Me.ruta_log() & Me._archivo, Me.z_f)
        Me.check_dir(Me.ruta_log() & "historico")
        Me.Thread_Zip = New Thread(AddressOf Me.Comprimir)
        Select Case Me.Thread_Zip.ThreadState
            Case ThreadState.Unstarted
                Me.Thread_Zip.Start()
        End Select
    End Sub

    ''' <summary>
    ''' Hilo que se encarga de la compresion del archivo
    ''' </summary>
    ''' <param name="fileName"></param>
    ''' <param name="zipFic"></param>
    ''' <param name="crearAuto"></param>
    ''' <remarks></remarks>
    Private Sub Comprimir(ByVal fileName As String, ByVal zipFic As String, Optional ByVal crearAuto As Boolean = False)
        Dim objCrc32 As New Crc32()
        Dim strmZipOutputStream As ZipOutputStream
        If zipFic = "" Then
            'zipFic = "."
            crearAuto = True
        End If
        If crearAuto Then
            zipFic = ruta_log() & "historico\" & DateTime.Now.ToString("yyMMddHHmmss") & ".zip"
        End If
        strmZipOutputStream = New ZipOutputStream(File.Create(zipFic))
        ' Compression Level: 0-9
        ' 0: no(Compression)
        ' 9: maximum compression
        strmZipOutputStream.SetLevel(6)
        Dim strFile As String = fileName

        Dim strmFile As FileStream = File.OpenRead(strFile)
        Dim abyBuffer(Convert.ToInt32(strmFile.Length - 1)) As Byte
        strmFile.Read(abyBuffer, 0, abyBuffer.Length)
        Dim sFile As String = Path.GetFileName(strFile)
        Dim theEntry As ZipEntry = New ZipEntry(sFile)

        Dim fi As New FileInfo(strFile)
        theEntry.DateTime = fi.LastWriteTime
        'theEntry.DateTime = DateTime.Now
        '
        theEntry.Size = strmFile.Length
        strmFile.Close()
        objCrc32.Reset()
        objCrc32.Update(abyBuffer)
        theEntry.Crc = objCrc32.Value
        strmZipOutputStream.PutNextEntry(theEntry)
        strmZipOutputStream.Write(abyBuffer, 0, abyBuffer.Length)

        strmZipOutputStream.Finish()
        strmZipOutputStream.Close()
    End Sub

    Private Sub Comprimir()
        Try
            Me.Comprimir(Me.ruta_log() & Me.z_f, "", True)
            File.Delete(Me.ruta_log() & Me.z_f)
        Catch ex As Exception
            Me.mensaje_log(ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Añade el texto espeifiado al archivo
    ''' </summary>
    ''' <param name="cadena">Texto a añadir</param>
    ''' <remarks></remarks>
    Public Sub escribir(ByVal cadena As String)
        Try
            Me.Zip()
            Dim objStreamWriter As StreamWriter
            objStreamWriter = New StreamWriter(Me.ruta_log() & Me._archivo, True)
            If Me.ColocarHora Then
                objStreamWriter.WriteLine(Now.ToString & " " & cadena)
            Else
                objStreamWriter.WriteLine(cadena)
            End If
            objStreamWriter.Close()
            objStreamWriter.Dispose()
        Catch ex As Exception
            Me.mensaje_log(ex.Message)
        End Try
    End Sub

End Class
