﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Laser.Orchard.UserReactions.Models {
    
    public class UserReactionsTypesRecord {

        public virtual int Id { get; set; }
        public virtual string TypeName { get; set; }
        public virtual int Priority { get; set; }
        public virtual string CssName { get; set; }
        public virtual bool Activating { get; set; }


        public static IEnumerable<UserReactionsTypesRecord> GetDefaultTypes() {
            return new[] { Angry, Boring, Exhausted, Happy, Joke, Kiss, Love, Pain, Sad, Shocked, Silent, Like, Iwasthere };
        }

        public static UserReactionsTypesRecord Angry {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "angry",
                    Priority = 1,
                    TypeCssClass = "glyph-icon flaticon-angry",
                    Activating = false
                };
            }
        }

        public static UserReactionsTypesRecord Boring {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "boring",
                    Priority = 2,
                    TypeCssClass = "glyph-icon flaticon-boring",
                    Activating = false
                };
            }
        }

        public static UserReactionsTypesRecord Exhausted {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "exhausted",
                    Priority = 3,
                    TypeCssClass = "glyph-icon flaticon-exhausted",
                    Activating = false
                };
            }
        }

        public static UserReactionsTypesRecord Happy {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "happy",
                    Priority = 4,
                    TypeCssClass = "glyph-icon flaticon-happy",
                    Activating = false
                };
            }
        }

        public static UserReactionsTypesRecord Joke {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "joke",
                    Priority = 5,
                    TypeCssClass = "glyph-icon flaticon-joke",
                    Activating = false
                };
            }
        }

        public static UserReactionsTypesRecord Kiss {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "kiss",
                    Priority = 6,
                    TypeCssClass = "glyph-icon flaticon-kiss",
                    Activating = false
                };
            }
        }


        public static UserReactionsTypesRecord Love {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "love",
                    Priority = 7,
                    TypeCssClass = "glyph-icon flaticon-love",
                    Activating = false
                };
            }
        }


        public static UserReactionsTypesRecord Pain {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "pain",
                    Priority = 8,
                    TypeCssClass = "glyph-icon flaticon-pain",
                    Activating = false
                };
            }
        }


        public static UserReactionsTypesRecord Sad {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "sad",
                    Priority = 9,
                    TypeCssClass = "glyph-icon flaticon-sad",
                    Activating = false
                };
            }
        }


        public static UserReactionsTypesRecord Shocked {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "shocked",
                    Priority = 10,
                    TypeCssClass = "glyph-icon flaticon-shocked",
                    Activating = false
                };
            }
        }


        public static UserReactionsTypesRecord Silent {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "silent",
                    Priority = 11,
                    TypeCssClass = "glyph-icon flaticon-silent",
                    Activating = false
                };
            }
        }


        public static UserReactionsTypesRecord Like {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "like",
                    Priority = 12,
                    TypeCssClass = "glyph-icon flaticon-like",
                    Activating = false
                };
            }
        }

        public static UserReactionsTypesRecord Iwasthere {
            get {
                return new UserReactionsTypesRecord {
                    TypeName = "iwasthere",
                    Priority = 13,
                    TypeCssClass = "glyph-icon flaticon-iwasthere",
                    Activating = false
                };
            }
        }

    }
}