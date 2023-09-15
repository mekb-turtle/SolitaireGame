using Solitaire.Properties;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using static Solitaire.SolitaireGame;

namespace Solitaire {
    public partial class Form1 : Form {
        private SolitaireGame game;

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            game = new SolitaireGame();
            MinimumSize = new Size(Width - ClientSize.Width + 577, Height - ClientSize.Height + 500);
            DoubleBuffered = true;
        }

        private void Form1_Paint(object sender, PaintEventArgs e) {
            e.Graphics.Clear(BackColor);
            GameRenderer.Render(e.Graphics, ClientSize, game, mouseCursor);
        }

        Point mouseCursor;

        private void Form1_Resize(object sender, EventArgs e) {
            Invalidate();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button != MouseButtons.Left) return;
            if (game.pickupCard != null) return;
            mouseCursor = new Point(e.X, e.Y);
            if (GameRenderer.GetStockCardPoint(game.stock.Count, ClientSize).Contains(mouseCursor)) {
                game.DrawStockCard(draw3box.Checked ? 3 : 1);
                return;
            }

            for (int i = 0, j = 0; i < game.tableau.Length; ++i, j = 0) {
                TableauCard card = game.tableau[i];
                if (!Card.IsCard(card)) continue;
                for (; ; ++j) {
                    if (card.visible && GameRenderer.GetTableauCardPoint(i, j, ClientSize).Contains(mouseCursor)) {
                        if (!Card.IsCard(card.above) || !GameRenderer.GetTableauCardPoint(i, j + 1, ClientSize).Contains(mouseCursor)) {
                            game.pickupCard = card;
                            game.pickupCardLocation = new CardLocation(CardLocation.CardLocationEnum.Tableau, i, j);
                            goto endMouseDown;
                        }
                    }
                    if (!Card.IsCard(card.above)) {
                        break;
                    }
                    card = card.above;
                }
            }
            if (game.waste.Count > 0) {
                Card card = game.waste[game.waste.Count - 1];
                if (Card.IsCard(card)) {
                    if (GameRenderer.GetWasteCardPoint(game.waste.Count > 3 ? 2 : game.waste.Count - 1, ClientSize).Contains(mouseCursor)) {
                        game.pickupCard = card;
                        game.pickupCardLocation = new CardLocation(CardLocation.CardLocationEnum.Waste, game.waste.Count - 1, 0);
                        goto endMouseDown;
                    }
                }
            }
            for (int i = 0; i < game.foundation.Length; ++i) {
                Card card = game.foundation[i];
                if (!Card.IsCard(card)) continue;
                if (GameRenderer.GetFoundationCardPoint(i, ClientSize).Contains(mouseCursor)) {
                    game.pickupCard = card;
                    game.pickupCardLocation = new CardLocation(CardLocation.CardLocationEnum.Foundation, i, 0);
                    goto endMouseDown;
                }
            }

endMouseDown:
            Invalidate();
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button != MouseButtons.Left) return;
            if (game.pickupCard == null) return;
            mouseCursor = new Point(e.X, e.Y);

            for (int i = 0, j = 0; i < game.tableau.Length; ++i, j = 0) {
                TableauCard card = game.tableau[i];
                if (!Card.IsCard(card)) {
                    if (GameRenderer.GetTableauCardPoint(i, j, ClientSize).Contains(mouseCursor)) {
                        game.MoveToCard(ref game.tableau[i]);
                        goto endMouseUp;
                    }
                    continue;
                }
                for (; ; ++j) {
                    if (card.visible && GameRenderer.GetTableauCardPoint(i, j, ClientSize).Contains(mouseCursor)) {
                        if (!Card.IsCard(card.above) || !GameRenderer.GetTableauCardPoint(i, j + 1, ClientSize).Contains(mouseCursor)) {
                            game.MoveToCard(ref card);
                            goto endMouseUp;
                        }
                    }
                    if (!Card.IsCard(card.above)) {
                        break;
                    }
                    card = card.above;
                }
            }
            for (int i = 0; i < game.foundation.Length; ++i) {
                if (GameRenderer.GetFoundationCardPoint(i, ClientSize).Contains(mouseCursor)) {
                    game.MoveToCard(ref game.foundation[i]);
                    goto endMouseUp;
                }
            }

endMouseUp:
            game.ClearPickup();
            Invalidate();
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e) {
            mouseCursor = new Point(e.X, e.Y);
            Invalidate();
        }

        private void restartButton_Click(object sender, EventArgs e) {
            if (MessageBox.Show("Do you want to start a new game?", "Restart game?", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                game.ResetGame();
            }
        }
    }
}
