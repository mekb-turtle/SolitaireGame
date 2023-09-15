using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Solitaire {
    public class SolitaireGame {
        public class Card {
            public enum Suite {
                Hearts, Diamonds, Clubs, Spades
            }
            public enum Rank {
                Ace, _2, _3, _4, _5, _6, _7, _8, _9, _10, Jack, Queen, King
            }

            public static bool SameSuiteColor(Suite a, Suite b) {
                return (a == Suite.Clubs || a == Suite.Spades) == (b == Suite.Clubs || b == Suite.Spades);
            }

            public Suite suite;
            public Rank rank;
            public bool active = false;
            public CardLocation location;

            virtual public void Unset() {
                active = false;
            }

            virtual public void Set(Suite suite, Rank rank) {
                this.suite = suite;
                this.rank = rank;
                active = true;
            }

            virtual public void Set(Card card) {
                Set(card.suite, card.rank);
            }

            virtual public void SetLocation(CardLocation location) {
                this.location = location;
            }

            public Card(CardLocation location) {
                SetLocation(location);
                Unset();
            }

            public Card(CardLocation location, Suite suite, Rank rank) {
                SetLocation(location);
                Set(suite, rank);
            }

            public Card(CardLocation location, Card card) {
                SetLocation(location);
                Set(card);
            }

            public static bool IsCard(Card card) {
                if (card == null) return false;
                return card.active;
            }

            public static string GetResourceName(Card card) {
                if (!IsCard(card)) return null;
                return card.GetResourceName();
            }

            virtual public string GetResourceName() {
                if (!active) return null;
                return "Card" + suite.ToString() + rank.ToString().Replace("_", "");
            }

            public static Card GetPreviousFoundationCard(Card card) {
                if (!IsCard(card)) return null;
                if (card.rank == Rank.Ace) return null;
                Card newCard = new Card(card.location, card);
                --newCard.rank;
                return newCard;
            }
        }

        public class TableauCard : Card {
            public bool visible = false;
            public TableauCard above = null;

            public override void Unset() {
                if (above != null) above.Unset();
                above = null;

                visible = false;
                base.Unset();
            }

            public void Set(bool visible, Suite suite, Rank rank) {
                this.visible = visible;
                this.suite = suite;
                this.rank = rank;
                active = true;
            }

            public void Set(bool visible, Card card) {
                this.visible = visible;
                Set(visible, card.suite, card.rank);
            }

            public override void SetLocation(CardLocation location) {
                base.SetLocation(location);
                if (IsCard(above)) {
                    above.SetLocation(location);
                }
            }

            public TableauCard(CardLocation location, bool visible, Suite suite, Rank rank) : base(location, suite, rank) {
                this.visible = visible;
            }

            public TableauCard(CardLocation location, bool visible, Card card) : base(location, card.suite, card.rank) {
                this.visible = visible;
            }

            public TableauCard(CardLocation location) : base(location) {
                visible = false;
            }

            override public string GetResourceName() {
                if (!active) return null;
                if (!visible) return "CardFlipped";
                return base.GetResourceName();
            }
        }

        public class CardLocation {
            public enum CardLocationEnum {
                Tableau, Foundation, Waste
            }

            public int column = 0, row = 0;
            public CardLocationEnum location;

            public CardLocation(CardLocationEnum location, int column, int row) {
                this.location = location;
                this.column = column;
                this.row = row;
            }
        }

        public Card pickupCard = null;
        public CardLocation pickupCardLocation = null;
        public Card[] foundation = new Card[4];
        public List<Card> waste = new List<Card>();
        public List<Card> stock = new List<Card>();
        public TableauCard[] tableau = new TableauCard[7];

        public SolitaireGame() {
            ResetGame();
        }

        public void ResetGame() {
            List<Card> cards = new List<Card>();
            {
                int i = 0;
                foreach (Card.Suite suite in Enum.GetValues(typeof(Card.Suite))) {
                    foreach (Card.Rank rank in Enum.GetValues(typeof(Card.Rank))) {
                        // initialize cards
                        cards.Add(new Card(null, suite, rank));
                        ++i;
                    }
                }
            }

            // shuffle card deck
            Random rng = new Random(Guid.NewGuid().GetHashCode());
            for (int i = cards.Count - 1; i > 0; --i) {
                int j = rng.Next(i + 1);
                Card temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }

            // clear foundation cards
            for (int i = 0; i < 4; ++i) {
                foundation[i] = null;
            }

            // clear waste and stock cards
            waste.Clear();
            stock.Clear();

            int cardIndex = 0;
            
            // reset tableau cards
            for (int column = 0; column < 7; ++column) {
                tableau[column]?.Unset();
                tableau[column] = null;
                TableauCard t = null;
                for (int row = 0; row <= column; ++row, ++cardIndex) {
                    CardLocation location = new CardLocation(CardLocation.CardLocationEnum.Tableau, column, row);
                    if (row == 0) {
                        tableau[column] = new TableauCard(location, row == column, cards[cardIndex]);
                        t = tableau[column];
                    } else {
                        t.above = new TableauCard(location,row == column, cards[cardIndex]);
                        t = t.above;
                    }
                }
            }

            // put rest of cards on stock pile
            for (int j = 0; cardIndex < cards.Count; ++cardIndex, ++j) {
                stock.Add(new Card(null, cards[cardIndex]));
            }
        }

        public void DrawStockCard(int times) {
            if (stock.Count > 0) {
                for (int i = 0; stock.Count > 0 && i < times; ++i) {
                    waste.Add(stock[stock.Count - 1]);
                    stock.RemoveAt(stock.Count - 1);
                }
            } else {
                while (waste.Count > 0) {
                    stock.Add(waste[waste.Count - 1]);
                    waste.RemoveAt(waste.Count - 1);
                }
            }
        }

        public bool FoundationCanMove(Card belowCard, Card aboveCard) {
            if (!Card.IsCard(aboveCard)) return false;
            if (aboveCard is TableauCard tableauCard) {
                if (Card.IsCard(tableauCard.above)) return false;
            }
            if (!Card.IsCard(belowCard)) return aboveCard.rank == Card.Rank.Ace;
            if (belowCard.rank == Card.Rank.King) return false;
            return aboveCard.suite == belowCard.suite && aboveCard.rank == belowCard.rank + 1;
        }

        public bool TableauCanMove(TableauCard belowCard, Card aboveCard) {
            if (!Card.IsCard(aboveCard)) return false;
            if (!Card.IsCard(belowCard)) return aboveCard.rank == Card.Rank.King;
            if (Card.IsCard(belowCard.above)) return false;
            if (belowCard.rank == Card.Rank.Ace) return false;
            return !Card.SameSuiteColor(aboveCard.suite, belowCard.suite) && aboveCard.rank == belowCard.rank - 1;
        }

        public bool MoveToCard(ref TableauCard card) {
            if (!TableauCanMove(card, pickupCard)) return false;
            if (pickupCard is TableauCard pickupTableauCard) {
                if (!Card.IsCard(card)) {
                    card = pickupTableauCard;
                } else {
                    if (card.above == pickupTableauCard) return false;
                    card.above = pickupTableauCard;
                }
            } else {
                if (!Card.IsCard(card)) {
                    card = new TableauCard(null, true, pickupCard);
                } else {
                    card.above = new TableauCard(null, true, pickupCard);
                }
            }
            card.SetLocation(card.location);
            RemoveOldMove();
            ClearPickup();
            ShowVisibleCards();
            return true;
        }

        public bool MoveToCard(ref Card card) {
            if (!FoundationCanMove(card, pickupCard)) return false;
            CardLocation loc = card?.location;
            if (pickupCard is TableauCard pickupTableauCard) {
                card = pickupTableauCard;
            } else {
                card = pickupCard;
            }
            card.location = loc;
            RemoveOldMove();
            ClearPickup();
            ShowVisibleCards();
            return true;
        }

        public void ShowVisibleCards() {
            foreach (TableauCard tableauCard_ in tableau) {
                TableauCard tableauCard = tableauCard_;
                if (Card.IsCard(tableauCard)) {
                    while (true) {
                        if (!Card.IsCard(tableauCard.above)) {
                            tableauCard.visible = true;
                            break;
                        }
                        tableauCard = tableauCard.above;
                    }
                }
            }
        }

        private void RemoveOldMove() {
            if (pickupCard == null || pickupCardLocation == null) return;
            switch (pickupCardLocation.location) {
                case CardLocation.CardLocationEnum.Tableau:
                    if (tableau[pickupCardLocation.column] == pickupCard) {
                        tableau[pickupCardLocation.column] = null;
                    } else {
                        TableauCard tableauCard = tableau[pickupCardLocation.column];
                        while (Card.IsCard(tableauCard)) {
                            if (tableauCard.above == pickupCard) {
                                tableauCard.above = null;
                                break;
                            }
                            tableauCard = tableauCard.above;
                        }
                    }
                    break;
                case CardLocation.CardLocationEnum.Foundation:
                    foundation[pickupCardLocation.column] = Card.GetPreviousFoundationCard(pickupCard);
                    break;
                case CardLocation.CardLocationEnum.Waste:
                    waste.Remove(pickupCard);
                    break;
            }
        }

        public void ClearPickup() {
            pickupCard = null;
            pickupCardLocation = null;
        }
    }
}
