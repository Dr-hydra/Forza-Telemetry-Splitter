# Forza Telemetry Splitter

[English](README.md) · [日本語](README.ja.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · **Español**

Envía la telemetría de Forza a varias herramientas a la vez.

La telemetría «Data Out» de Forza Horizon 6 solo puede enviarse a una IP y un puerto. Eso obliga a
elegir: alimentar [VirtualTCU](https://github.com/Forza-Love/fh6-virtual_tcu) (cambio de marchas
automático), una herramienta de ajuste o un panel — pero no todas a la vez.

Forza Telemetry Splitter se sitúa en medio. Recibe la telemetría de Forza en su propio puerto y reenvía
cada paquete, sin modificar, a tantas herramientas locales como quieras. La latencia añadida es inferior
a un milisegundo y los datos no se alteran, así que cada herramienta funciona igual que si hablara
directamente con Forza.

Sin afiliación ni respaldo de Turn 10, Playground Games o Microsoft. «Forza» es una marca de Microsoft.

## Funciones

| Función | Descripción |
|---------|-------------|
| Distribución | Distribuye la telemetría de Forza a cualquier número de destinos, sin modificar los paquetes. |
| Multijuego | Compatible con Forza Horizon 4/5/6 y Forza Motorsport (7, 2023). El juego se detecta automáticamente. |
| Superposición de estado | Una pequeña etiqueta arriba a la derecha muestra «Conectado / Sin datos» junto con la marcha y la velocidad en directo. |
| Multilingüe | Inglés, japonés, francés, alemán y español. Se selecciona automáticamente según el idioma de Windows. |
| App en la bandeja | Funciona discretamente en la bandeja del sistema, como VirtualTCU. |
| Sin administrador | Solo UDP local: sin aviso de UAC. |

## Instalación

Recomendado — el instalador:

1. Descarga `ForzaTelemetrySplitterInstaller.exe` desde la página de [Releases](../../releases).
2. Clic derecho → Propiedades → marca «Desbloquear» abajo en la pestaña General → Aceptar. Así evitas la
   pantalla «Windows protegió tu PC».
3. Ejecútalo. La instalación es por usuario, así que no hay aviso de administrador. Ofrece un acceso
   directo en el escritorio y una opción «Iniciar automáticamente con Windows».
4. Al terminar, se inicia en la bandeja del sistema.

Avanzado / sin instalación: descarga `ftsPortable.exe` y ejecútalo directamente. Para la mayoría se
recomienda el instalador anterior.

## Configuración en el juego

1. Abre la app desde la bandeja. Escucha en el puerto **44405** y ya está configurada para reenviar a
   VirtualTCU (su puerto habitual 5555).
2. En tu juego de Forza, abre Data Out (en Horizon: Configuración → HUD y jugabilidad → Data Out):
   - Data Out: activado
   - Dirección IP: `127.0.0.1`
   - Puerto: **`44405`**
   - Formato de paquete: Car Dash (Horizon) o Dash (Motorsport)
3. Deja tus otras herramientas como están: el distribuidor reenvía a cada una en su puerto habitual.
   Para añadir una herramienta, pulsa «Agregar» en la app.
4. Conduce: la etiqueta de arriba a la derecha se pone verde y todas las herramientas activas reciben
   los datos.

## Más información (en inglés)

- [Compilar desde el código fuente](docs/BUILDING.md)
- [Aviso de Windows SmartScreen](docs/SMARTSCREEN.md)
- [Informar de un error](docs/REPORTING-BUGS.md)
- [Contribuir](CONTRIBUTING.md)
- [Licencia (MIT)](LICENSE)

## Probado en

Windows 10 y 11. Forza Horizon 4/5/6 y Forza Motorsport (7, 2023) — detectado automáticamente.
