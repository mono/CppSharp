#include "MainWindow.h"
#include <qmessagebox.h>
#include <qwindow.h>
#include <qfilesystemmodel.h>
#include <qstringlist.h>
#include "AstModel.h"

class UpdateLock
{
public:
    UpdateLock(bool &lock) : myLock(lock)
    {
        myLock = true;
    }
    ~UpdateLock()
    {
        myLock = false;
    }
    bool & myLock;
};

MainWindow::MainWindow(QWidget *parent) : 
    QMainWindow(parent),
    isUpdateInProgress(false)
{
    myUi.setupUi(this);

    connect(myUi.actionRefresh, &QAction::triggered, this, &MainWindow::RefreshAst);

    myHighlighter = new Highlighter(myUi.codeViewer->document());
    myUi.nodeProperties->setHeaderLabels({ "Property", "Value" });
    connect(myUi.codeViewer, &QTextEdit::cursorPositionChanged, this, &MainWindow::HighlightNodeMatchingCode);
    connect(myUi.codeViewer, &QTextEdit::textChanged, this, &MainWindow::OnCodeChange);
    connect(myUi.showDetails, &QPushButton::clicked, this, &MainWindow::ShowNodeDetails);
}

void MainWindow::RefreshAst()
{
    auto ast = myReader.readAst(myUi.codeViewer->document()->toPlainText().toStdString(),
        myUi.commandLineArgs->document()->toPlainText().toStdString());
    auto model = new AstModel(std::move(ast));

    myUi.astTreeView->setModel(model);
    myUi.astTreeView->setRootIndex(model->rootIndex());
    connect(myUi.astTreeView->selectionModel(), &QItemSelectionModel::currentChanged,
        this, &MainWindow::HighlightCodeMatchingNode);
    connect(myUi.astTreeView->selectionModel(), &QItemSelectionModel::currentChanged,
        this, &MainWindow::DisplayNodeProperties);
    myUi.astTreeView->setEnabled(myReader.ready());
}

void MainWindow::HighlightCodeMatchingNode(const QModelIndex &newNode, const QModelIndex &previousNode)
{
    if (isUpdateInProgress)
    {
        return;
    }
    auto lock = UpdateLock{ isUpdateInProgress };
    auto node = myUi.astTreeView->model()->data(newNode, Qt::NodeRole).value<GenericAstNode*>();
    auto &manager = myReader.getManager();
    std::pair<int, int> location;
    if (!node->getRangeInMainFile(location, manager, myReader.getContext()))
    {
        return;
    }
    auto cursor = myUi.codeViewer->textCursor();
    cursor.setPosition(location.first);
    cursor.setPosition(location.second, QTextCursor::KeepAnchor);
    myUi.codeViewer->setTextCursor(cursor);
}

void MainWindow::DisplayNodeProperties(const QModelIndex &newNode, const QModelIndex &previousNode)
{
    myUi.nodeProperties->clear();
    auto node = myUi.astTreeView->model()->data(newNode, Qt::NodeRole).value<GenericAstNode*>();
    for (auto &prop : node->getProperties())
    {
        new QTreeWidgetItem(myUi.nodeProperties, QStringList{ QString::fromStdString(prop.first), QString::fromStdString(prop.second) });
    }
    myUi.showDetails->setVisible(node->hasDetails);
}

void MainWindow::HighlightNodeMatchingCode()
{
    if (isUpdateInProgress || !myReader.ready())
    {
        return;
    }
    auto lock = UpdateLock{ isUpdateInProgress };
    auto cursorPosition = myUi.codeViewer->textCursor().position();
    auto nodePath = myReader.getBestNodeMatchingPosition(cursorPosition);
    auto model = myUi.astTreeView->model();
    if (!nodePath.empty())
    {
        auto currentIndex = model->index(0, 0); // Returns the root
        currentIndex = model->index(0, 0, currentIndex); // Returns the AST node
        auto currentNode = nodePath.front();
        bool first = true;
        for (auto node : nodePath)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                auto index = currentNode->findChildIndex(node);
                if (index == -1)
                {
                    // Something wrong, just silently return
                    return;
                }
                currentIndex = model->index(index, 0, currentIndex);
                currentNode = node;
            }
        }
        myUi.astTreeView->scrollTo(currentIndex, QAbstractItemView::EnsureVisible);
        auto selectionModel = myUi.astTreeView->selectionModel();
//        selectionModel->select(currentIndex, QItemSelectionModel::ClearAndSelect);
        selectionModel->setCurrentIndex(currentIndex, QItemSelectionModel::ClearAndSelect);
        DisplayNodeProperties(currentIndex, currentIndex); // Since we won't use the previous node, it's not an issue if it is wrong...
    }
}

void MainWindow::ShowNodeDetails()
{
    auto selectionModel = myUi.astTreeView->selectionModel();
    auto model = myUi.astTreeView->model();
    auto node = myUi.astTreeView->model()->data(selectionModel->currentIndex(), Qt::NodeRole).value<GenericAstNode*>();
    if (! node || !node->hasDetails)
    {
        QMessageBox::warning(this, windowTitle() + " - Error in details",
            "The currently selected node does not have details", QMessageBox::Ok);
        return;
    }
    if (node->details.empty())
    {
        node->details = node->detailsComputer();
    }

    auto win = new QDialog(this);
    win->setLayout(new QGridLayout());
    win->resize(size());
    win->move(pos());
    win->setWindowTitle(windowTitle() + " - " + QString::fromStdString(node->name) + " - " + QString::fromStdString(node->detailsTitle));
    auto edit = new QTextEdit(win);
    win->layout()->addWidget(edit);
    edit->setText(QString::fromStdString(node->details));
    edit->setReadOnly(true);
    myDetailWindows.push_back(win);
    win->show();
}


void MainWindow::closeEvent(QCloseEvent *event)
{
    for (auto win : myDetailWindows)
    {
        win->close();
    }
    event->accept();
}

void MainWindow::OnCodeChange()
{
    myUi.astTreeView->setEnabled(false);
    myReader.dirty();
}

