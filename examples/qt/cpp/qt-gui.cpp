
/* 
 * These are not in the unix version of qt, and -fkeep-inline-functions will generate
 * references to their vtables etc., making the library unloadable.
 */
#define QT_NO_STYLE_WINDOWSVISTA
#define QT_NO_STYLE_WINDOWSXP
#define QT_NO_STYLE_S60
#define QT_NO_STYLE_WINDOWSCE
#define QT_NO_STYLE_WINDOWSMOBILE
#define QT_NO_QWSEMBEDWIDGET

// this one was annoying to track down!
#define QT_NO_TRANSLATION

#include "QtGui/QApplication"
#include "QtGui/QPushButton"

int main ()
{
}
