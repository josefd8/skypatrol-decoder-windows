Imports System.Net
Imports Libreria.ClassGeneral
Imports Libreria.ClassTCPServer
Imports decoderTrama

Public Class Form1

    ''' <summary>
    ''' Servidor TCP desde donde se reciben mensajes de los equipos
    ''' </summary>
    ''' <remarks></remarks>
    Private WithEvents ServidorTCP As New libreria.ClassTCPServer

    ''' <summary>
    ''' Servidor UDP desde donde se reciben mensajes de los equipos
    ''' </summary>
    ''' <remarks></remarks>
    Private WithEvents ServidorUDP As libreria.UDP_Server

    ''' <summary>
    ''' Los que almacena toda la data cruda recibida en cualquiera de los Sockets (TCP o UDP) en formato binario
    ''' </summary>
    ''' <remarks></remarks>
    Private log_datos As New libreria.LogFile("log_datos")

    ''' <summary>
    ''' Log que almacena cualquier error que se haya producido durante la ejecucion del programa
    ''' </summary>
    ''' <remarks></remarks>
    Private log_errores As New libreria.LogFile("log_errores")

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles ButtonConectar.Click

        'Dependiendo del texto del boton y de si el usuario selecciono UDP o TCP, se inicia o detiene el socket correspondiente
        If ButtonConectar.Text = "Conectar" Then

            Try
                If Not (IsNumeric(TextBoxPuerto.Text)) Then
                    ToolStripStatusLabel1.Text = ("El puerto de conexión no parece tener un formato válido")
                    Return
                Else
                    If CLng(TextBoxPuerto.Text) > 65535 Then
                        ToolStripStatusLabel1.Text = ("El puerto de conexión no parece tener un formato válido")
                        Return
                    End If
                End If

                ServidorUDP = New libreria.UDP_Server


                If RadioButtonUDP.Checked Then
                    ServidorUDP.Iniciar(IPAddress.Parse(TextBoxIP.Text), TextBoxPuerto.Text)

                    If Not ServidorUDP.Estado Then
                        ButtonConectar.Text = "Conectar"
                        Me.desbloquear_controles()
                    Else
                        ButtonConectar.Text = "Desconectar"
                        ToolStripStatusLabel1.Text = ("Escuchando en modo UDP por " & TextBoxIP.Text & ":" & TextBoxPuerto.Text)
                        Me.bloquear_controles()
                    End If

                Else
                    ServidorTCP.IpDeEscucha = IPAddress.Parse(TextBoxIP.Text)
                    ServidorTCP.PuertoDeEscucha = TextBoxPuerto.Text
                    ServidorTCP.Escuchar(1)

                    If Not ServidorTCP.Estado Then
                        ButtonConectar.Text = "Conectar"
                        Me.desbloquear_controles()
                    Else
                        ButtonConectar.Text = "Desconectar"
                        ToolStripStatusLabel1.Text = ("Escuchando en modo TCP por " & TextBoxIP.Text & ":" & TextBoxPuerto.Text)
                        Me.bloquear_controles()
                    End If
                End If

            Catch ex As Exception
                ToolStripStatusLabel1.Text = ("Se encontró un error: " & ex.Message)
                log_errores.escribir("Inicio conexion: " & ex.Message)
                Me.desbloquear_controles()
            End Try

        Else

            If RadioButtonUDP.Checked Then
                ServidorUDP.Cerrar()
            Else
                ServidorTCP.Cerrar()
            End If

            ButtonConectar.Text = "Conectar"
            ToolStripStatusLabel1.Text = ("Escucha detenida")
            Me.desbloquear_controles()
            ListView1.Items.Clear()
            LabelEquiposConectados.Text = "0 Equipos Conectados"
        End If

    End Sub

    ''' <summary>
    ''' Bloquea lo controles durante el proceso de conexion.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub bloquear_controles()

        Me.TextBoxIP.Enabled = False
        Me.TextBoxPuerto.Enabled = False
        Me.RadioButtonTCP.Enabled = False
        Me.RadioButtonUDP.Enabled = False

    End Sub

    ''' <summary>
    ''' Desbloquea los controles a la hora de la desonexion
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub desbloquear_controles()

        Me.TextBoxIP.Enabled = True
        Me.TextBoxPuerto.Enabled = True
        Me.RadioButtonTCP.Enabled = True
        Me.RadioButtonUDP.Enabled = True

    End Sub

    Private Sub ServidorTCP_ConexionTerminada(IDTerminal As IPEndPoint) Handles ServidorTCP.ConexionTerminada

        Dim direccion As String = IDTerminal.Address.ToString & ":" & IDTerminal.Port

        For Each itemx As ListViewItem In ListView1.Items
            If itemx.SubItems(1).Text = direccion Then
                itemx.Remove()
            End If
        Next

    End Sub

    Private Sub Error_Generado(ByVal Mensaje As String) Handles ServidorTCP.Error_Generado
        ToolStripStatusLabel1.Text = ("Se encontró un error: " & Mensaje)
    End Sub

    Private Sub Error_Socket(ByVal Mensaje As String) Handles ServidorUDP.Error_Socket
        ToolStripStatusLabel1.Text = ("Se encontró un error: " & Mensaje)
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing

        'Si se cierra el form, se guardan las variables de la interfaz
        Try
            My.Settings.IP = TextBoxIP.Text
            My.Settings.Puerto = TextBoxPuerto.Text

            If RadioButtonUDP.Checked Then
                My.Settings.Protocolo = "UDP"
            Else
                My.Settings.Protocolo = "TCP"
            End If

            My.Settings.Formato = ComboBoxTipoDato.Text
            My.Settings.Convertir = ComboBoxConvertir.SelectedItem
            My.Settings.DatoEnvio = TextBoxDatosEnvio.Text
            If CheckBoxACK.CheckState = 1 Then
                My.Settings.ACK = True
            Else
                My.Settings.ACK = False
            End If


            If Not ServidorUDP Is Nothing Then
                If ServidorUDP.Estado Then
                    ServidorUDP.Cerrar()
                End If
            End If

            If Not ServidorTCP Is Nothing Then
                If ServidorTCP.Estado Then
                    ServidorTCP.Cerrar()
                End If
            End If

            Me.Dispose()

        Catch ex As Exception

        End Try

    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        'Al iniciarse el From, se cargan las variables en la interfaz

        CheckForIllegalCrossThreadCalls = False

        ToolStripStatusLabel1.Text = "Programa iniciado"

        ListView1.Columns.Add("Nombre", 100, HorizontalAlignment.Center)
        ListView1.Columns.Add("Dirección Remota", 100, HorizontalAlignment.Center)
        ListView1.Columns.Add("Fecha", 100, HorizontalAlignment.Center)
        ListView1.Columns.Add("Mensajes", 100, HorizontalAlignment.Center)

        LabelEquiposConectados.Text = "0 Equipos Conectados"

        TextBoxIP.Text = My.Settings.IP
        TextBoxPuerto.Text = My.Settings.Puerto

        If My.Settings.Protocolo = "UDP" Then
            RadioButtonUDP.Checked = True
        Else
            RadioButtonTCP.Checked = True
        End If

        ComboBoxTipoDato.Items.Add("String")
        ComboBoxTipoDato.Items.Add("Binario")

        ComboBoxConvertir.Items.Add("Binario/String")
        ComboBoxConvertir.Items.Add("String/Binario")

        ComboBoxTipoDato.SelectedItem = My.Settings.Formato
        ComboBoxConvertir.SelectedItem = My.Settings.Convertir

        If My.Settings.ACK Then
            CheckBoxACK.CheckState = CheckState.Checked
        Else
            CheckBoxACK.CheckState = CheckState.Unchecked
        End If

        TextBoxDatosEnvio.Text = My.Settings.DatoEnvio

    End Sub

    Private Sub datos_recibidos_full(ByVal datos As Paquete_Entrante) Handles ServidorTCP.Datos_Recibidos

        'Los mensajes transmitidos por TCP por alguna razon vienen con una cabecera extra (00,61) que no la decodifica
        'bien la clase de skypatrol, por eso creo un nuevo array en donde esos primeros bytes no esten.

        Dim nuevos_datos() As Byte = {}
        Dim contador As Long = 0

        For Each b As Byte In datos.Datos_Byte

            If contador > 1 Then
                ReDim Preserve nuevos_datos(contador - 2)
                nuevos_datos(contador - 2) = datos.Datos_Byte(contador)
            End If

            contador = contador + 1

        Next

        datos.Datos_Byte = nuevos_datos

        procesar_datos(datos)
    End Sub

    Private Sub Datos_recibidos(ByVal Datos As Paquete_Entrante) Handles ServidorUDP.Datos_Recibidos
        procesar_datos(Datos)
    End Sub

    ''' <summary>
    ''' Toma los datos que hayan llegado al Socket (sea por TCP o por UDP) y los procesa
    ''' </summary>
    ''' <param name="Datos"></param>
    ''' <remarks></remarks>
    Private Sub procesar_datos(ByVal Datos As Paquete_Entrante)

        Try
            Dim registro As New libreria.BaseDatos.Registro
            Dim decoder As New decoderTrama.SkyPatrol(Datos.Datos_Byte)

            decoder.Decodificar()
            registro = decoder.Registro_Vehiculo

            If Not registro.Id_Vehiculo Is Nothing Then

                incluir_en_lista(registro, Datos)

                'Si hay un objeto seleccionado en la lista, se muestra detalle
                If ListView1.SelectedItems.Count > 0 Then

                    Dim selected_item As ListViewItem
                    selected_item = ListView1.SelectedItems(0)


                    If selected_item.Text = registro.Id_Vehiculo Then

                        TextBoxDetalle.AppendText("**********Nuevo mensaje a las " & Now & "**********" & vbCrLf)
                        TextBoxDetalle.AppendText("ID: " & registro.Id_Vehiculo & vbCrLf)
                        TextBoxDetalle.AppendText("Latitud: " & registro.Latitud.Coordenada & " " & registro.Latitud.Cardinalidad & vbCrLf)
                        TextBoxDetalle.AppendText("Longitud: " & registro.Longitud.Coordenada & " " & registro.Longitud.Cardinalidad & vbCrLf)
                        TextBoxDetalle.AppendText("Altitud: " & registro.Altitud & ", Dirección: " & registro.Direccion & vbCrLf)
                        TextBoxDetalle.AppendText("Fecha de reporte: " & registro.Fecha & ", Satélites: " & registro.Nro_Satelites & vbCrLf)
                        TextBoxDetalle.AppendText("Velocidad: " & registro.Velocidad & ", Evento: " & registro.Num_Evento & vbCrLf)
                        TextBoxDetalle.AppendText("Puertos Entrada: '" & registro.In_Puertos & "', Puertos Salida: '" & registro.Out_Puertos & "'" & vbCrLf)
                        TextBoxDetalle.AppendText("****************************Fin de Mensaje****************************" & vbCrLf & vbCrLf)

                    End If


                End If


                'En este punto puede crearse una rutina para guardar datos en la BD

            End If

            'Si el usuario selecciono que debe enviarse Acknowledgement, se envia la palabra "ACK" de regreso al equipo Skypatrol
            'Para que este sepa que el mensaje se recibio con exito. Si no lo recibe, el equipo se mantendra enviando el mismo mensaje
            If CheckBoxACK.CheckState = CheckState.Checked And RadioButtonUDP.Checked Then
                Dim bytes() As Byte = {"&H00", "&H0A", "&H01", "&H00", "&H41", "&H43", "&H4B"}
                ServidorUDP.Enviar(bytes, Datos.Ip, Datos.Puerto.ToString)
            End If

            Dim cliente As String = Datos.Ip & ":" & Datos.Puerto.ToString
            log_datos.escribir(registro.Id_Vehiculo & " " & cliente & " " & byte_array_to_String(Datos.Datos_Byte))

        Catch ex As Exception
            ToolStripStatusLabel1.Text = "procesar_datos: " & ex.Message
            log_errores.escribir("procesar_datos: " & ex.Message)
        End Try

    End Sub

    ''' <summary>
    ''' Convierte una cadena de bytes en String (numeros separados por comas)
    ''' </summary>
    ''' <param name="data"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function byte_array_to_String(ByVal data As Byte()) As String

        Dim salida As String
        Dim arraytemp() As String = {}
        Dim contador As Long = 0

        For Each b As Byte In data
            ReDim Preserve arraytemp(contador)
            arraytemp(contador) = Hex(b)
            contador = contador + 1
        Next

        salida = Join(arraytemp, ",")
        Return salida

    End Function

    ''' <summary>
    ''' Busca si el equipo que transmite un mensaje ya se encuentra en la lista. Si ya esta, actualiza los datos (fecha del reporte, IP
    ''' remota y numero de mensajes), si no, se agrega
    ''' </summary>
    ''' <param name="registro">Objeto tipo registro con la informacion del mensaje transmitido por el Skypatrol ya decodificada</param>
    ''' <param name="Datos">Informacion cruda del mensaje entrante</param>
    ''' <remarks></remarks>
    Private Sub incluir_en_lista(ByVal registro As libreria.BaseDatos.Registro, ByVal Datos As Paquete_Entrante)

        Try
            'Si el elemento no esta en la lista se agrega, si ya esta, se actualizan los datos
            Dim seEncuentra As Boolean = False

            For Each itemx As ListViewItem In ListView1.Items

                If itemx.Text = registro.Id_Vehiculo Then
                    seEncuentra = True
                    itemx.SubItems(1).Text = Datos.Ip & ":" & Datos.Puerto.ToString
                    itemx.SubItems(2).Text = Now.ToString
                    itemx.SubItems(3).Text = CLng(itemx.SubItems(3).Text) + 1
                End If

            Next

            If Not seEncuentra Then
                Dim str(3) As String
                str(0) = registro.Id_Vehiculo
                str(1) = Datos.Ip & ":" & Datos.Puerto.ToString
                str(2) = Now.ToString
                str(3) = CStr(1)
                Dim item As New ListViewItem(str)
                ListView1.Items.Add(item)
            End If


            LabelEquiposConectados.Text = ListView1.Items.Count & " Equipos Conectados"

        Catch ex As Exception
            ToolStripStatusLabel1.Text = "incluir_en_lista: " & ex.Message
            log_errores.escribir("incluir_en_lista: " & ex.Message)
        End Try

    End Sub

    Private Sub ButtonBorrar_Click(sender As Object, e As EventArgs) Handles ButtonBorrar.Click
        TextBoxDetalle.Clear()
    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        Me.ajustarAnchoColumnas()
    End Sub

    Private Sub ajustarAnchoColumnas()
        For Each column As ColumnHeader In ListView1.Columns
            column.Width = -2
        Next
    End Sub

    ''' <summary>
    ''' Realiza una busqueda en la tabla segun los caracteres introducidos por el usuario.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs) Handles TextBoxBuscar.TextChanged
        Try
            Dim coincidencias As Long = 0

            If TextBoxBuscar.Text <> "" Then
                For Each itemx As ListViewItem In ListView1.Items
                    If InStr(itemx.Text.ToUpper, TextBoxBuscar.Text.ToUpper) Then
                        itemx.ForeColor = Color.Black
                        coincidencias = coincidencias + 1
                    Else
                        itemx.ForeColor = Color.Gray
                    End If
                Next

            Else
                For Each itemx As ListViewItem In ListView1.Items
                    itemx.ForeColor = Color.Black
                Next

            End If

            If coincidencias > 0 Then
                LabelCoincidencias.Text = CStr(coincidencias) & " coincidencias"
            Else
                LabelCoincidencias.Text = ""
            End If

        Catch ex As Exception
            ToolStripStatusLabel1.Text = "Se encontró un error al buscar el dispositivo: " & ex.Message
            log_errores.escribir("Busqueda: " & ex.Message)
        End Try


    End Sub

    Private Sub ListView1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListView1.ItemSelectionChanged

        LabelEquipo.Text = ""
        LabelDireccion.Text = ""

        For Each itemx As ListViewItem In ListView1.Items
            If itemx.Selected = True Then
                LabelEquipo.Text = itemx.Text
                LabelDireccion.Text = itemx.SubItems(1).Text
            End If

        Next

    End Sub

    ''' <summary>
    ''' Realiza la conversion de los datos segun los parametros introducidos por el usuario
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub ButtonConvertir_Click(sender As Object, e As EventArgs) Handles ButtonConvertir.Click

        Dim texto As String = TextBoxDatosEnvio.Text

        If texto <> "" Then

            If ComboBoxConvertir.Text = "Binario/String" Then

                Dim bytes() As Byte = string_2_byte(texto)

                If Not bytes Is Nothing Then
                    TextBoxDatosEnvio.Text = System.Text.ASCIIEncoding.ASCII.GetString(string_2_byte(texto))
                End If

            Else
                Try
                    Dim bytes() As Byte = System.Text.Encoding.ASCII.GetBytes(texto)
                    Dim salida As String = ""

                    For Each b As Byte In bytes
                        salida = salida & "," & CStr(b)
                    Next

                    TextBoxDatosEnvio.Text = salida.Substring(1, salida.Length - 1)
                Catch ex As Exception
                    ToolStripStatusLabel1.Text = ("Se encontró un error durante la conversión: " & ex.Message)
                End Try

            End If

        End If

    End Sub

    ''' <summary>
    ''' Realiza el envio de los datos introducidos por el usuario a el equipo remoto
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub ButtonEnviar_Click_1(sender As Object, e As EventArgs) Handles ButtonEnviar.Click

        Try

            If LabelDireccion.Text <> "" And ComboBoxTipoDato.Text <> "" Then

                Dim IP As String = LabelDireccion.Text
                Dim formato As String = ComboBoxTipoDato.Text
                Dim mensaje As String = TextBoxDatosEnvio.Text
                Dim direccion() As String = Split(IP, ":")

                If RadioButtonUDP.Checked Then

                    If formato = "String" Then
                        ServidorUDP.Enviar(System.Text.Encoding.ASCII.GetBytes(mensaje), direccion(0), direccion(1))
                    Else

                        Dim bytes() As Byte = string_2_byte(mensaje)

                        If Not bytes Is Nothing Then
                            ServidorUDP.Enviar(string_2_byte(mensaje), direccion(0), direccion(1))
                        End If

                    End If

                Else

                    Dim IDCliente As New Net.IPEndPoint(System.Net.IPAddress.Parse(direccion(0)), Convert.ToInt32(direccion(1)))

                    If formato = "String" Then
                        ServidorTCP.EnviarDatos(IDCliente, mensaje)
                    Else

                        Dim bytes() As Byte = string_2_byte(mensaje)

                        If Not bytes Is Nothing Then
                            ServidorTCP.EnviarDatos(IDCliente, string_2_byte(mensaje))
                        End If

                    End If

                End If

            End If

        Catch ex As Exception
            log_errores.escribir("Envio: " & ex.Message)
            ToolStripStatusLabel1.Text = ("Se encontró un error durante el envío de datos: " & ex.Message)
        End Try

    End Sub

    ''' <summary>
    ''' Toma un String que representa una cadena de bytes (numeros separados por coma), y los convierte a un arreglo de elementos
    ''' tipo byte para ser enviados por el Socket.
    ''' </summary>
    ''' <param name="texto"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function string_2_byte(ByVal texto As String) As Byte()

        Try
            Dim numeros() As String = Split(texto, ",")
            Dim bytes() As Byte
            Dim contador As Long = 0

            For Each numero As String In numeros

                ReDim Preserve bytes(contador)
                bytes(contador) = "&H" & Hex(numero)
                contador = contador + 1
            Next

            Return bytes

        Catch ex As Exception
            log_errores.escribir("string_2_byte: " & ex.Message)
            ToolStripStatusLabel1.Text = ("Los datos de origen no tienen formato correcto para la conversión")
            Return Nothing
        End Try

    End Function

End Class
