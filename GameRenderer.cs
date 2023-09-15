using Solitaire.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Solitaire.SolitaireGame;

namespace Solitaire {
    public static class GameRenderer {
        private static readonly int cardWidth = 71;
        private static readonly int cardHeight = 96;
        private static readonly int cardXGap = cardWidth + 8;
        private static readonly int cardYGap = cardHeight + 8;
        private static readonly int tableauMargin = 16;
        private static readonly int margin = 16;
        private static readonly int totalWidth = (margin * 2) + (cardXGap * 6) + cardWidth;
        private static readonly string emptyCard = "CardEmpty";
        private static readonly string flippedCard = "CardFlipped";
        private static readonly string highlightedCard = "CardHighlighted";
        
        private static int StretchXPosition(int X, Size size) {
            return (int)((float)(X + (cardWidth * 0.5)) / totalWidth * size.Width - (cardWidth * 0.5));
        }

        public static Rectangle GetFoundationCardPoint(int i, Size size) {
            return new Rectangle(StretchXPosition(cardXGap * (i + 3) + margin, size), margin, cardWidth, cardHeight);
        }

        public static Rectangle GetWasteCardPoint(int i, Size size) {
            return new Rectangle(StretchXPosition(cardXGap + (i * 32) + margin, size), margin, cardWidth, cardHeight);
        }

        public static Rectangle GetStockCardPoint(int i, Size size) {
            return new Rectangle(StretchXPosition((int)(i * 0.5) + margin, size), margin, cardWidth, cardHeight);
        }

        public static Rectangle GetTableauCardPoint(int i, int j, Size size) {
            return new Rectangle(StretchXPosition(i * cardXGap + margin, size), margin + cardYGap + (j * tableauMargin), cardWidth, cardHeight);
        }

        public static Rectangle GetCursorCardPoint(Point cursor) {
            return new Rectangle(cursor.X - (int)(cardWidth * 0.5), cursor.Y - (int)(cardHeight * 0.5), cardWidth, cardHeight);
        }

        private static Image GetImage(String name) {
            if (name == null) return null;
            object resource = Resources.ResourceManager.GetObject(name);
            if (resource is Image image) return image;
            return null;
        }

        public static void Render(Graphics g, Size size, SolitaireGame game, Point cursor) {
            for (int i = 0; i < game.foundation.Length; ++i) {
                Card card = game.foundation[i];
                if (game.pickupCard == game.foundation[i]) {
                    card = Card.GetPreviousFoundationCard(game.foundation[i]);
                }
                string name = Card.GetResourceName(card);
                if (name == null) name = emptyCard;
                Image image = GetImage(name);
                if (image != null) {
                    Rectangle rect = GetFoundationCardPoint(i, size);
                    g.DrawImage(image, rect);
                    if (game.FoundationCanMove(card, game.pickupCard))
                        g.DrawImage(GetImage(highlightedCard), ExtendRect(rect));
                }
            }
            if (game.stock.Count == 0) {
                g.DrawImage(GetImage(emptyCard), GetStockCardPoint(0, size));
            }
            for (int i = 0; i < game.stock.Count; ++i) {
                if (!Card.IsCard(game.stock[i])) continue;
                g.DrawImage(GetImage(flippedCard), GetStockCardPoint(i + 1, size));
            }
            for (int i = game.waste.Count > 3 ? game.waste.Count - 3 : 0, j = 0; i < game.waste.Count; ++i, ++j) {
                if (game.pickupCard == game.waste[i]) {
                    continue;
                }
                string name = Card.GetResourceName(game.waste[i]);
                Image image = GetImage(name);
                if (image != null) g.DrawImage(image, GetWasteCardPoint(j, size));
            }
            for (int i = 0, j = 0; i < game.tableau.Length; ++i, j = 0) {
                TableauCard card = game.tableau[i];
                if (!Card.IsCard(card)) {
                    Rectangle rect = GetTableauCardPoint(i, j, size);
                    if (game.TableauCanMove(card, game.pickupCard))
                        g.DrawImage(GetImage(highlightedCard), ExtendRect(rect));
                    continue;
                }
                for (; ; ++j) {
                    if (game.pickupCard != card) {
                        string name = Card.GetResourceName(card);
                        Image image = GetImage(name);
                        if (image != null) {
                            Rectangle rect = GetTableauCardPoint(i, j, size);
                            g.DrawImage(image, rect);
                            if (game.TableauCanMove(card, game.pickupCard))
                                g.DrawImage(GetImage(highlightedCard), ExtendRect(rect));
                        }
                    } else break;
                    if (!Card.IsCard(card.above)) break;
                    card = card.above;
                }
            }
            if (Card.IsCard(game.pickupCard)) {
                Rectangle rect = GetCursorCardPoint(cursor);
                if (game.pickupCard is TableauCard tableauCard) {
                    for (int j = 0; ; ++j) {
                        string name = Card.GetResourceName(tableauCard);
                        Image image = GetImage(name);
                        if (image != null) {
                            g.DrawImage(image, rect);
                            if (game.TableauCanMove(tableauCard, game.pickupCard))
                                g.DrawImage(GetImage(highlightedCard), ExtendRect(rect));
                            rect.Y += tableauMargin;
                        }
                        if (!Card.IsCard(tableauCard.above)) break;
                        tableauCard = tableauCard.above;
                    }
                } else {
                    string name = Card.GetResourceName(game.pickupCard);
                    Image image = GetImage(name);
                    if (image != null) {
                        g.DrawImage(image, rect);
                        if (game.pickupCardLocation?.location == CardLocation.CardLocationEnum.Foundation
                            && game.FoundationCanMove(game.pickupCard, game.pickupCard))
                            g.DrawImage(GetImage(highlightedCard), ExtendRect(rect));
                    }
                }
            }
        }

        private static Rectangle ExtendRect(Rectangle rectangle) {
            return new Rectangle(rectangle.X - 1, rectangle.Y - 1, rectangle.Width + 2, rectangle.Height + 2);
        }
    }
}
